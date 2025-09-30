
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReplenishmentAlertController : ControllerBase
    {
        private readonly IReplenishmentAlert _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public ReplenishmentAlertController(
            IReplenishmentAlert repository,
            IMapper mapper,
            ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔓 Get all alerts
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            var alerts = _repository.GetAll().ToList();
            var response = _mapper.Map<List<ReplenishmentAlertResponse>>(alerts);
            return Ok(response);
        }

        // 🔓 Get alert by ID
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var alert = await _repository.GetByIdAsync(id);
            if (alert == null)
                return NotFound(new { message = $"Alert ID {id} not found." });

            var response = _mapper.Map<ReplenishmentAlertResponse>(alert);
            return Ok(response);
        }

        // 🔐 Create alert manually
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReplenishmentAlertCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
                        .ToList()
                });

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            if (!await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId))
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

            var entity = _mapper.Map<ReplenishmentAlert>(request);
            entity.CreatedAt = DateTime.UtcNow;
            await _repository.AddAsync(entity);

            // 🧠 Log to InventoryReport
            var shelfState = await _context.ProductShelves
                .FirstOrDefaultAsync(ps => ps.ProductId == entity.ProductId && ps.ShelfId == entity.ShelfId);

            if (shelfState != null)
            {
                bool reportExists = await _context.InventoryReports.AnyAsync(r =>
                    r.ProductId == entity.ProductId &&
                    r.ShelfId == entity.ShelfId &&
                    r.ReportDate == DateTime.Today);

                if (!reportExists)
                {
                    var report = new InventoryReport
                    {
                        ProductId = entity.ProductId,
                        ShelfId = entity.ShelfId,
                        ReportDate = DateTime.Today,
                        QuantityOnShelf = shelfState.Quantity,
                        QuantityRestocked = null,
                        AlertTriggered = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InventoryReports.Add(report);
                    await _context.SaveChangesAsync();
                }
            }

            var response = _mapper.Map<ReplenishmentAlertResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.AlertId }, new
            {
                message = "Replenishment alert created successfully.",
                data = response
            });
        }

        // 🔐 Delete alert with confirmation
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            if (!confirm)
                return BadRequest(new
                {
                    message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                });

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Alert ID {id} not found." });

            try
            {
                await _repository.DeleteAsync(id);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
            {
                return Conflict(new
                {
                    message = $"Cannot delete Alert ID {id} because it is referenced in other records (e.g., RestockTask)."
                });
            }

            return Ok(new { message = $"Replenishment alert ID {id} deleted successfully." });
        }

        [Authorize(Roles = "manager")]
        [HttpPost("trigger-all")]
        public async Task<IActionResult> TriggerFullReplenishmentFlow()
        {
            var velocityData = await (
                from sale in _context.SalesHistories
                group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
                select new
                {
                    daily.Key.ProductId,
                    DailySales = daily.Sum(x => x.Quantity)
                }
            ).ToListAsync();

            var salesVelocity = velocityData
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SalesVelocity = g.Average(x => x.DailySales)
                }).ToDictionary(x => x.ProductId, x => x.SalesVelocity);

            var shelves = await _context.ProductShelves
                .Include(ps => ps.Shelf)
                .ToListAsync();

            int alertsCreated = 0;
            int requestsCreated = 0;
            int tasksAssigned = 0;

            foreach (var ps in shelves)
            {
                if (!salesVelocity.ContainsKey(ps.ProductId)) continue;

                var velocity = salesVelocity[ps.ProductId];
                if (velocity < 0.1) continue;

                var daysToDepletion = Math.Round(ps.Quantity / velocity, 2);
                if (daysToDepletion >= 10) continue;

                var urgency = daysToDepletion switch
                {
                    <= 1 => "critical",
                    <= 2 => "high",
                    <= 4 => "medium",
                    _ => "low"
                };

                // Check for existing alert
                var alert = await _context.ReplenishmentAlerts
                    .FirstOrDefaultAsync(a =>
                        a.ProductId == ps.ProductId &&
                        a.ShelfId == ps.ShelfId &&
                        a.Status == "open");

                if (alert == null)
                {
                    alert = new ReplenishmentAlert
                    {
                        ProductId = ps.ProductId,
                        ShelfId = ps.ShelfId,
                        PredictedDepletionDate = DateTime.Today.AddDays(Math.Round(daysToDepletion)),
                        UrgencyLevel = urgency,
                        Status = "open",
                        CreatedAt = DateTime.Now
                    };

                    _context.ReplenishmentAlerts.Add(alert);
                    await _context.SaveChangesAsync();
                    alertsCreated++;
                }

                // Check for existing stock request
                bool requestExists = await _context.StockRequests.AnyAsync(r =>
                    r.ProductId == ps.ProductId &&
                    r.StoreId == ps.Shelf.StoreId &&
                    r.DeliveryStatus == "pending");

                if (!requestExists)
                {
                    var quantityNeeded = ps.Shelf.Capacity - ps.Quantity;
                    if (quantityNeeded > 0)
                    {
                        var request = new StockRequest
                        {
                            ProductId = ps.ProductId,
                            StoreId = ps.Shelf.StoreId,
                            Quantity = quantityNeeded,
                            RequestDate = DateTime.Now,
                            DeliveryStatus = "pending"
                        };

                        _context.StockRequests.Add(request);
                        alert.Status = "converted";
                        await _context.SaveChangesAsync();
                        requestsCreated++;
                    }
                }

                // Check for existing restock task
                bool taskExists = await _context.RestockTasks.AnyAsync(t =>
                    t.ProductId == ps.ProductId &&
                    t.ShelfId == ps.ShelfId &&
                    t.Status == "pending");

                if (!taskExists)
                {
                    var staff = await _context.Staffs
                        .Where(s => s.StoreId == ps.Shelf.StoreId)
                        .FirstOrDefaultAsync();

                    if (staff != null)
                    {
                        var task = new RestockTask
                        {
                            AlertId = alert.AlertId,
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            AssignedTo = staff.StaffId,
                            Status = "pending",
                            AssignedAt = DateTime.Now
                        };

                        _context.RestockTasks.Add(task);
                        await _context.SaveChangesAsync();
                        tasksAssigned++;
                    }
                }
            }

            return Ok(new
            {
                message = "Full replenishment flow triggered.",
                alertsCreated,
                requestsCreated,
                tasksAssigned
            });
        }
    }
}
 
