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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var reports = await _repository.GetAll().ToListAsync();

                var enriched = new List<InventoryReportResponse>();

                foreach (var report in reports)
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

                return Ok(new
                {
                    message = "Inventory reports retrieved successfully.",
                    data = enriched
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving inventory reports.", details = ex.Message });
            }
        }

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

        [Authorize(Roles = "manager")] // 🔐 Only managers can create reports
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InventoryReportCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
                if (!productExists)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                var shelfExists = await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId);
                if (!shelfExists)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

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
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("IX_InventoryReport_ProductId_ShelfId_ReportDate") == true)
                {
                    return Conflict(new
                    {
                        message = "Inventory report for this product, shelf, and date already exists."
                    });
                }

                return Conflict(new
                {
                    message = "Database error while creating inventory report.",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while creating inventory report.", details = ex.Message });
            }
        }

        [Authorize(Roles = "manager")] // 🔐 Only managers can update reports
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

                var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
                if (!productExists)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                var shelfExists = await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId);
                if (!shelfExists)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                _mapper.Map(request, existing);
                await _repository.UpdateAsync(existing);
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("IX_InventoryReport_ProductId_ShelfId_ReportDate") == true)
                {
                    return Conflict(new
                    {
                        message = "Inventory report for this product, shelf, and date already exists."
                    });
                }

                return Conflict(new
                {
                    message = "Database error while updating inventory report.",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while updating inventory report.", details = ex.Message });
            }
        }

        [Authorize(Roles = "manager")] // 🔐 Only managers can delete reports
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
    }
}
