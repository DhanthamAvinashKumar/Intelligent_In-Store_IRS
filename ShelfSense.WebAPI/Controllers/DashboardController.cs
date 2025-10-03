using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ShelfSenseDbContext _context;

        public DashboardController(ShelfSenseDbContext context)
        {
            _context = context;
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

                var response = new List<DashboardInventoryReportResponse>();

                foreach (var ps in productShelves)
                {
                    try
                    {
                        var latestRestock = await _context.RestockTasks
                            .Where(rt => rt.ProductId == ps.ProductId && rt.ShelfId == ps.ShelfId && rt.Status == "completed")
                            .OrderByDescending(rt => rt.CompletedAt)
                            .FirstOrDefaultAsync();

                        var alertExists = await _context.ReplenishmentAlerts
                            .AnyAsync(ra => ra.ProductId == ps.ProductId && ra.ShelfId == ps.ShelfId && ra.Status != "closed");

                        response.Add(new DashboardInventoryReportResponse
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            ReportDate = DateTime.Today,
                            QuantityOnShelf = ps.Quantity,
                            //QuantityRestocked = latestRestock?.QuantityRestocked,
                            AlertTriggered = alertExists
                        });
                    }
                    catch (Exception innerEx)
                    {
                        // If one shelf fails, continue with others but log/return partial info
                        response.Add(new DashboardInventoryReportResponse
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            ReportDate = DateTime.Today,
                            QuantityOnShelf = ps.Quantity,
                            AlertTriggered = false
                        });
                        // Optionally log innerEx here
                    }
                }

                return Ok(new
                {
                    message = "Dashboard inventory summary retrieved successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error generating dashboard summary.",
                    details = ex.Message
                });
            }
        }
    }
}
