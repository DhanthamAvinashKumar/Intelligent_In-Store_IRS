using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "warehouse")] // ✅ Only warehouse role can access
    public class WarehouseController : ControllerBase
    {
        private readonly ShelfSenseDbContext _context;
        private readonly IMapper _mapper;

        public WarehouseController(ShelfSenseDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔍 Get all pending stock requests
        [HttpGet("pending-requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var requests = await _context.StockRequests
                    .Where(r => r.DeliveryStatus == "requested" || r.DeliveryStatus == "pending")
                    .ToListAsync();

                var response = _mapper.Map<List<StockRequestResponse>>(requests);

                return Ok(new
                {
                    message = "Pending stock requests retrieved successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving pending stock requests.", details = ex.Message });
            }
        }

        // 🚚 Mark a stock request as "in_transit"
        [HttpPut("{id}/dispatch")]
        public async Task<IActionResult> MarkAsInTransit(long id, [FromBody] DateTime? estimatedArrival)
        {
            try
            {
                var request = await _context.StockRequests.FirstOrDefaultAsync(r => r.RequestId == id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered")
                    return BadRequest(new { message = "This request is already delivered." });

                if (request.DeliveryStatus == "in_transit")
                    return BadRequest(new { message = "This request is already in transit." });

                request.DeliveryStatus = "in_transit";
                request.EstimatedTimeOfArrival = estimatedArrival ?? DateTime.UtcNow.AddDays(2);

                _context.StockRequests.Update(request);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Stock request ID {id} marked as in_transit.",
                    data = new
                    {
                        request.RequestId,
                        request.ProductId,
                        request.StoreId,
                        request.Quantity,
                        request.DeliveryStatus,
                        request.EstimatedTimeOfArrival
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating stock request {id} to in_transit.", details = ex.Message });
            }
        }

        // 📦 Mark a stock request as "delivered"
        [HttpPut("{id}/deliver")]
        public async Task<IActionResult> MarkAsDelivered(long id)
        {
            try
            {
                var request = await _context.StockRequests.FirstOrDefaultAsync(r => r.RequestId == id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered")
                    return BadRequest(new { message = "This request is already delivered." });

                if (request.DeliveryStatus != "in_transit")
                    return BadRequest(new { message = "Request must be in_transit before it can be delivered." });

                request.DeliveryStatus = "delivered";
                request.EstimatedTimeOfArrival = DateTime.UtcNow;

                // ✅ Update shelf quantity
                var shelf = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .FirstOrDefaultAsync(ps => ps.ProductId == request.ProductId && ps.Shelf.StoreId == request.StoreId);

                if (shelf != null)
                {
                    shelf.Quantity += request.Quantity;
                    _context.ProductShelves.Update(shelf);

                    await LogInventoryReportAsync(request.ProductId, shelf.ShelfId, request.Quantity, false);
                }

                _context.StockRequests.Update(request);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Stock request ID {id} marked as delivered and shelf updated.",
                    data = new
                    {
                        request.RequestId,
                        request.ProductId,
                        request.StoreId,
                        request.Quantity,
                        request.DeliveryStatus,
                        
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error marking stock request {id} as delivered.", details = ex.Message });
            }
        }

        // ❌ Cancel a stock request
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelRequest(long id, [FromBody] string? reason)
        {
            try
            {
                var request = await _context.StockRequests.FirstOrDefaultAsync(r => r.RequestId == id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered")
                    return BadRequest(new { message = "Delivered requests cannot be cancelled." });

                if (request.DeliveryStatus == "cancelled")
                    return BadRequest(new { message = "This request is already cancelled." });

                request.DeliveryStatus = "cancelled";
                request.EstimatedTimeOfArrival = null;

                _context.StockRequests.Update(request);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Stock request ID {id} has been cancelled.",
                    data = new
                    {
                        request.RequestId,
                        request.ProductId,
                        request.StoreId,
                        request.Quantity,
                        request.DeliveryStatus,
                        CancelReason = reason ?? "Not specified"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error cancelling stock request {id}.", details = ex.Message });
            }
        }

        // 🔧 Internal helper to log inventory report
        private async Task LogInventoryReportAsync(long productId, long shelfId, int? quantityRestocked, bool alertTriggered)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
