using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System.Net;

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
            try
            {
                var alerts = _repository.GetAll().ToList();
                var response = _mapper.Map<List<ReplenishmentAlertResponse>>(alerts);
                return Ok(new { message = "Replenishment alerts retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving replenishment alerts.", details = ex.Message });
            }
        }

        // 🔓 Get alert by ID
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var alert = await _repository.GetByIdAsync(id);
                if (alert == null)
                    return NotFound(new { message = $"Alert ID {id} not found." });

                var response = _mapper.Map<ReplenishmentAlertResponse>(alert);
                return Ok(new { message = "Replenishment alert retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving alert {id}.", details = ex.Message });
            }
        }

        // 🔐 Create alert manually
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReplenishmentAlertCreateRequest request)
        {
            try
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
                        r.ReportDate == DateTime.Today); // Use DateTime.Today (local) for date component comparison

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
                            CreatedAt = DateTime.UtcNow // Use UTC for timestamps
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating replenishment alert.", details = ex.Message });
            }
        }

        // 🔐 Delete alert with confirmation
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            try
            {
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                    });
                }

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Unexpected error while deleting Alert ID {id}.",
                    details = ex.Message
                });
            }
        }

        // 🧠 AUTOMATED FULL REPLENISHMENT WORKFLOW 🧠
        [HttpPost("trigger-all")]
        public async Task<IActionResult> TriggerFullReplenishmentFlow()
        {
            int alertsCreated = 0;
            int requestsCreated = 0;
            int tasksAssigned = 0;
            var errors = new List<object>();

            try
            {
                // --- 1. Calculate Sales Velocity (Average Daily Sales) ---
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
                    .ToDictionary(
                        x => x.Key,
                        g => g.Average(x => x.DailySales)
                    );

                // --- 2. Get All Shelves with Product Info ---
                var shelves = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .ToListAsync();

                // Track alerts that were fulfilled to avoid re-triggering them later in the loop
                var fulfilledAlerts = new List<long>();

                // --- 3. Iterate and Process ---
                foreach (var ps in shelves)
                {
                    try
                    {
                        // Filter 1: Product must have a sales velocity > 0.1
                        if (!salesVelocity.ContainsKey(ps.ProductId) || salesVelocity[ps.ProductId] < 0.1)
                        {
                            continue;
                        }

                        var velocity = salesVelocity[ps.ProductId];
                        var daysToDepletion = Math.Round((double)ps.Quantity / velocity, 2);
                        var storeId = ps.Shelf.StoreId;
                        var quantityNeeded = ps.Shelf.Capacity - ps.Quantity;

                        // Filter 2: Only act if depletion is critical (< 3 days)
                        if (daysToDepletion >= 3)
                        {
                            continue;
                        }

                        // Determine Urgency (based on the original logic)
                        var urgency = daysToDepletion switch
                        {
                            <= 1 => "critical",
                            <= 2 => "high",
                            < 3 => "medium",
                            _ => "low" // Should not hit here due to filter 2
                        };

                        // --- A. Create/Find Alert ---
                        var alert = await _context.ReplenishmentAlerts
                            .FirstOrDefaultAsync(a =>
                                a.ProductId == ps.ProductId &&
                                a.ShelfId == ps.ShelfId &&
                                a.Status == "open");

                        if (alert == null)
                        {
                            // If no open alert exists, create one
                            alert = new ReplenishmentAlert
                            {
                                ProductId = ps.ProductId,
                                ShelfId = ps.ShelfId,
                                PredictedDepletionDate = DateTime.Today.AddDays(Math.Round(daysToDepletion)),
                                UrgencyLevel = urgency,
                                Status = "open",
                                CreatedAt = DateTime.UtcNow // Use UTC for timestamps
                            };

                            _context.ReplenishmentAlerts.Add(alert);
                            await _context.SaveChangesAsync();
                            alertsCreated++;
                        }

                        // --- B. Convert Alert to Stock Request (YOUR KEY REQUIREMENT) ---
                        if (quantityNeeded > 0)
                        {
                            // Check for existing pending stock request for this Product/Store pair
                            bool requestExists = await _context.StockRequests.AnyAsync(r =>
                                r.ProductId == ps.ProductId &&
                                r.StoreId == storeId &&
                                r.DeliveryStatus == "requested"); // Use "requested" as the correct pending status

                            if (!requestExists)
                            {
                                // Create the new Stock Request
                                var request = new StockRequest
                                {
                                    ProductId = ps.ProductId,
                                    StoreId = storeId,
                                    Quantity = quantityNeeded,
                                    RequestDate = DateTime.UtcNow,
                                    DeliveryStatus = "requested"
                                };

                                _context.StockRequests.Add(request);
                                requestsCreated++;

                                // **CRITICAL FIX: Fulfill the Alert that triggered the need**
                                if (alert.Status == "open") // Only update if it's currently open
                                {
                                    alert.Status = "completed";
                                    alert.FulfillmentNote = $"Auto-requested via trigger-all on {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                                    _context.ReplenishmentAlerts.Update(alert);
                                    fulfilledAlerts.Add(alert.AlertId); // Track fulfillment
                                }

                                await _context.SaveChangesAsync();
                            }
                            // Else: Request already exists, so the alert remains "open" or "completed" 
                            // (depending on its previous state) to avoid spamming the warehouse.
                        }

                        // --- C. Assign Restock Task ---
                        // Only assign a task if a request was successfully placed OR if the alert is still open (high urgency)
                        // Use the ID of the alert that was either found or created
                        var currentAlertId = alert.AlertId;

                        bool taskExists = await _context.RestockTasks.AnyAsync(t =>
                            t.ProductId == ps.ProductId &&
                            t.ShelfId == ps.ShelfId &&
                            t.Status == "pending");

                        if (!taskExists && currentAlertId > 0 &&
                            (requestsCreated > 0 || urgency == "critical" || urgency == "high"))
                        {
                            var staff = await _context.Staffs
                                .Where(s => s.StoreId == storeId)
                                .FirstOrDefaultAsync();

                            if (staff != null)
                            {
                                var task = new RestockTask
                                {
                                    AlertId = currentAlertId,
                                    ProductId = ps.ProductId,
                                    ShelfId = ps.ShelfId,
                                    AssignedTo = staff.StaffId,
                                    Status = "pending",
                                    AssignedAt = DateTime.UtcNow
                                };

                                _context.RestockTasks.Add(task);
                                await _context.SaveChangesAsync();
                                tasksAssigned++;
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        // **ERROR REPORTING FIX:** Log the specific failure and continue, 
                        // but ensure the outer method reports the errors.
                        errors.Add(new
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            Message = $"Inner processing error: {innerEx.Message}",
                            Details = innerEx.InnerException?.Message
                        });
                    }
                }

                // --- 4. Final Success Response ---
                return Ok(new
                {
                    message = "Full replenishment flow triggered.",
                    alertsFound = alertsCreated + fulfilledAlerts.Count,
                    requestsCreated,
                    tasksAssigned,
                    errorsReported = errors.Count,
                    errors
                });
            }
            catch (Exception ex)
            {
                // --- 5. Final Critical Error Response ---
                return StatusCode(500, new
                {
                    message = "Critical error during full replenishment flow setup.",
                    details = ex.Message,
                    errorsReported = errors.Count,
                    errors // Include any item-specific errors collected
                });
            }
        }

        [Authorize]
        [HttpGet("closed")] // New route to separate active from closed
        public async Task<IActionResult> GetAllClosedAlerts()
        {
            try
            {
                var closedAlerts = await _context.ClosedReplenishmentAlerts
                    // 🌟 OPTIONAL ENHANCEMENT: Use .Include() if your DTO needs Product/Shelf details 🌟
                    // .Include(a => a.Product)
                    // .Include(a => a.Shelf)
                    .OrderByDescending(a => a.ClosedAt)
                    .ToListAsync();

                // This assumes ClosedAlertResponse and its AutoMapper profile are correctly defined.
                var response = _mapper.Map<List<ClosedAlertResponse>>(closedAlerts);

                return Ok(new
                {
                    message = "Closed replenishment alerts retrieved successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                // Log the exception details here for debugging purposes (recommended)
                // _logger.LogError(ex, "Error retrieving closed alerts.");

                return StatusCode((int)HttpStatusCode.InternalServerError, new
                {
                    message = "Error retrieving closed alerts.",
                    details = ex.Message
                });
            }
        }
    }
}