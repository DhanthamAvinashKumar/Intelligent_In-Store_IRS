using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestockFrequencyController : ControllerBase
    {
        private readonly ShelfSenseDbContext _context;

        public RestockFrequencyController(ShelfSenseDbContext context)
        {
            _context = context;
        }

        // 🔐 Manager-only access
        [Authorize(Roles = "manager")]
        [HttpGet("summary")]
        public async Task<IActionResult> GetRestockFrequencySummary()
        {
            var result = await _context.ReplenishmentAlerts
                .GroupBy(r => new { r.ProductId, r.ShelfId })
                .Select(g => new RestockFrequencyDto
                {
                    ProductId = (int)g.Key.ProductId,
                    ShelfId = (int)g.Key.ShelfId,
                    AlertCount = g.Count(),
                    TotalDays = EF.Functions.DateDiffDay(
                        g.Min(r => r.CreatedAt),
                        g.Max(r => r.CreatedAt)
                    ),
                    AvgRestockFrequencyDays = Math.Round(
                        g.Count() == 0 ? 0 :
                        (double)EF.Functions.DateDiffDay(
                            g.Min(r => r.CreatedAt),
                            g.Max(r => r.CreatedAt)
                        ) / g.Count(), 2)
                })
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    message = "No restock frequency data available.",
                    data = new List<RestockFrequencyDto>()
                });
            }

            return Ok(new
            {
                message = "Restock frequency summary retrieved successfully.",
                data = result
            });
        }
    }
}
