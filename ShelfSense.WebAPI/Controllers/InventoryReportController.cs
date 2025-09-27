using AutoMapper;
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
        public IActionResult GetAll()
        {
            try
            {
                var reports = _repository.GetAll().ToList();
                var response = _mapper.Map<List<InventoryReportResponse>>(reports);
                return Ok(response);
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

                var response = _mapper.Map<InventoryReportResponse>(report);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving the inventory report.", details = ex.Message });
            }
        }

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
                await _repository.AddAsync(entity);

                var response = _mapper.Map<InventoryReportResponse>(entity);
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
