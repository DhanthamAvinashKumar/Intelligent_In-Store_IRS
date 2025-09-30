 
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
    [Authorize]
    public class StockRequestController : ControllerBase
    {
        private readonly IStockRequest _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public StockRequestController(IStockRequest repository, IMapper mapper, ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔍 Get all stock requests
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _repository.GetAll().ToListAsync();
            var response = _mapper.Map<List<StockRequestResponse>>(requests);
            return Ok(new { message = "Stock requests retrieved successfully.", data = response });
        }

        // 🔍 Get request by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return NotFound(new { message = $"Stock request ID {id} not found." });

            var response = _mapper.Map<StockRequestResponse>(request);
            return Ok(response);
        }

        // 📝 Create new stock request and log inventory report
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockRequestCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            var entity = _mapper.Map<StockRequest>(request);
            entity.RequestDate = DateTime.UtcNow;
            entity.DeliveryStatus = "pending";

            await _repository.AddAsync(entity);

            // 🧠 Log to InventoryReport
            var shelfId = await _context.ProductShelves
                .Where(ps => ps.ProductId == request.ProductId && ps.Shelf.StoreId == request.StoreId)
                .Select(ps => ps.ShelfId)
                .FirstOrDefaultAsync();

            if (shelfId != 0)
                await LogInventoryReportAsync(request.ProductId, shelfId, null, false);

            var response = _mapper.Map<StockRequestResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.RequestId }, response);
        }

        // ✏️ Update delivery status
        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] StockRequestCreateRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Stock request ID {id} not found." });

            existing.DeliveryStatus = request.DeliveryStatus;
            existing.EstimatedTimeOfArrival = request.EstimatedTimeOfArrival;

            await _repository.UpdateAsync(existing);

            // 🧠 Log delivery to InventoryReport if status is delivered
            if (request.DeliveryStatus == "delivered")
            {
                var shelf = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .FirstOrDefaultAsync(ps => ps.ProductId == existing.ProductId && ps.Shelf.StoreId == existing.StoreId);

                if (shelf != null)
                {
                    shelf.Quantity += existing.Quantity;
                    _context.ProductShelves.Update(shelf);

                    await LogInventoryReportAsync(existing.ProductId, shelf.ShelfId, existing.Quantity, false);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Stock request updated successfully." });
        }

        private async Task LogInventoryReportAsync(long productId, long shelfId, int? quantityRestocked, bool alertTriggered)
        {
            var shelfState = await _context.ProductShelves
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.ShelfId == shelfId);

            if (shelfState == null) return;

            bool reportExists = await _context.InventoryReports.AnyAsync(r =>
                r.ProductId == productId &&
                r.ShelfId == shelfId &&
                r.ReportDate == DateTime.Today);

            if (!reportExists)
            {
                var report = new InventoryReport
                {
                    ProductId = productId,
                    ShelfId = shelfId,
                    ReportDate = DateTime.Today,
                    QuantityOnShelf = shelfState.Quantity,
                    QuantityRestocked = quantityRestocked,
                    AlertTriggered = alertTriggered,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryReports.Add(report);
                await _context.SaveChangesAsync();
            }
        }

        // 🗑️ Delete request
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Stock request ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        // 🔧 Internal method to log inventory report
        
    }
}
