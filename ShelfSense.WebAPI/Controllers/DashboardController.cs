//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace ShelfSense.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class DashboardController : ControllerBase
//    {
//        [Authorize(Roles = "manager")]
//        [HttpGet("manager-dashboard")]
//        public IActionResult GetManagerDashboard() =>
//            Ok("Visible to managers only");

//        [Authorize(Roles = "staff")]
//        [HttpGet("staff-tasks")]
//        public IActionResult GetStaffTasks() =>
//            Ok("Visible to staff only");

//        [Authorize(Roles = "manager,staff")]
//        [HttpGet("shared-tasks")]
//        public IActionResult GetSharedTasks() =>
//            Ok("Visible to both managers and staff");
//    }
//}

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

                return Ok(new
                {
                    message = "Dashboard inventory summary retrieved successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating dashboard summary.", details = ex.Message });
            }
        }
    }
}
