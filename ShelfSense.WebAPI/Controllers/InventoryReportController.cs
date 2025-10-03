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
    [Authorize] // 🔐 Require authentication for all endpoints
    public class InventoryReportController : ControllerBase
    {
        private readonly IInventoryReport _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public InventoryReportController(IInventoryReport repository, IMapper mapper, ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔍 Get all reports with enrichment
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var reports = await _repository.GetAll().ToListAsync();
                var enriched = new List<InventoryReportResponse>();

                foreach (var report in reports)
                {
                    try
                    {
                        var shelf = await _context.Shelves
                            .Where(s => s.ShelfId == report.ShelfId)
                            .Select(s => new { s.Capacity })
                            .FirstOrDefaultAsync();

                        var response = _mapper.Map<InventoryReportResponse>(report);
                        response.ShelfCapacity = shelf?.Capacity ?? 0;
                        response.UtilizationPercent = shelf != null && shelf.Capacity > 0
                            ? Math.Round(report.QuantityOnShelf * 100.0 / shelf.Capacity, 2)
                            : 0;

                        enriched.Add(response);
                    }
                    catch (Exception innerEx)
                    {
                        // If enrichment for one report fails, continue with others
                        enriched.Add(_mapper.Map<InventoryReportResponse>(report));
                        // Optionally log innerEx here
                    }
                }

                return Ok(new
                {
                    message = "Inventory reports retrieved successfully.",
                    data = enriched
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving inventory reports.",
                    details = ex.Message
                });
            }
        }





        // 🔍 Get single report by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var report = await _repository.GetByIdAsync(id);
                if (report == null)
                    return NotFound(new { message = $"Report ID {id} not found." });

                var shelf = await _context.Shelves
                    .Where(s => s.ShelfId == report.ShelfId)
                    .Select(s => new { s.Capacity })
                    .FirstOrDefaultAsync();

                var response = _mapper.Map<InventoryReportResponse>(report);
                response.ShelfCapacity = shelf?.Capacity ?? 0;
                response.UtilizationPercent = shelf != null && shelf.Capacity > 0
                    ? Math.Round(report.QuantityOnShelf * 100.0 / shelf.Capacity, 2)
                    : 0;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving the inventory report.", details = ex.Message });
            }
        }

        // 📝 Create report manually or from restock logic
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InventoryReportCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                if (!await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId))
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                bool exists = await _context.InventoryReports.AnyAsync(r =>
                    r.ProductId == request.ProductId &&
                    r.ShelfId == request.ShelfId &&
                    r.ReportDate == request.ReportDate.Date);

                if (exists)
                    return Conflict(new { message = "Inventory report for this product, shelf, and date already exists." });

                var entity = _mapper.Map<InventoryReport>(request);
                entity.CreatedAt = DateTime.UtcNow;

                await _repository.AddAsync(entity);

                var shelf = await _context.Shelves
                    .Where(s => s.ShelfId == entity.ShelfId)
                    .Select(s => new { s.Capacity })
                    .FirstOrDefaultAsync();

                var response = _mapper.Map<InventoryReportResponse>(entity);
                response.ShelfCapacity = shelf?.Capacity ?? 0;
                response.UtilizationPercent = shelf != null && shelf.Capacity > 0
                    ? Math.Round(entity.QuantityOnShelf * 100.0 / shelf.Capacity, 2)
                    : 0;

                return CreatedAtAction(nameof(GetById), new { id = response.ReportId }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while creating inventory report.", details = ex.Message });
            }
        }

        // ✏️ Update report
        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] InventoryReportCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Report ID {id} not found." });

                if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                if (!await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId))
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                _mapper.Map(request, existing);
                await _repository.UpdateAsync(existing);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while updating inventory report.", details = ex.Message });
            }
        }

        // 🗑️ Delete report
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Report ID {id} not found." });

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting inventory report.", details = ex.Message });
            }
        }

        [HttpGet("inventory-summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            try
            {
                var productShelves = await _context.ProductShelves
                    .Include(ps => ps.Product)
                    .Include(ps => ps.Shelf)
                    .ToListAsync();

                var response = new List<InventoryReportCreateRequest>();

                foreach (var ps in productShelves)
                {
                    var latestRestock = await _context.RestockTasks
                        .Where(rt => rt.ProductId == ps.ProductId && rt.ShelfId == ps.ShelfId && rt.Status == "completed")
                        .OrderByDescending(rt => rt.CompletedAt)
                        .FirstOrDefaultAsync();

                    var latestStockRequest = await _context.StockRequests
                        .Where(sr => sr.ProductId == ps.ProductId && sr.StoreId == sr.StoreId)
                        .OrderByDescending(sr => sr.RequestDate)
                        .FirstOrDefaultAsync();


                    var alertExists = await _context.ReplenishmentAlerts
                        .AnyAsync(ra => ra.ProductId == ps.ProductId && ra.ShelfId == ps.ShelfId && ra.Status != "closed");

                    var today = DateTime.Today;

                    // Check if report already exists
                    bool exists = await _context.InventoryReports.AnyAsync(r =>
                        r.ProductId == ps.ProductId &&
                        r.ShelfId == ps.ShelfId &&
                        r.ReportDate == today);

                    if (!exists)
                    {
                        var report = new InventoryReport
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            ReportDate = today,
                            QuantityOnShelf = ps.Quantity,
                            QuantityRestocked = latestStockRequest?.Quantity,
                            AlertTriggered = alertExists,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.InventoryReports.Add(report);
                    }

                    response.Add(new InventoryReportCreateRequest
                    {
                        ProductId = ps.ProductId,
                        ShelfId = ps.ShelfId,
                        ReportDate = today,
                        QuantityOnShelf = ps.Quantity,
                        QuantityRestocked = latestStockRequest?.Quantity,
                        AlertTriggered = alertExists
                    });
                }

                await _context.SaveChangesAsync(); // Persist all new reports

                return Ok(new
                {
                    message = "Dashboard inventory summary retrieved and stored successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating and storing dashboard summary.", details = ex.Message });
            }
        }

    }
}
