using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Infrastructure.Data;
// Adjust if your DbContext is elsewhere

namespace ShelfSense.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestockFrequencyController : ControllerBase
    {
        private readonly ShelfSenseDbContext _context;

        public RestockFrequencyController(ShelfSenseDbContext context)
        {
            _context = context;
        }

        [HttpGet("restock-frequency")]
        public async Task<IActionResult> GetRestockFrequency()
        {
            var result = await _context.ReplenishmentAlerts
                .GroupBy(r => new { r.ProductId, r.ShelfId })
                .Select(g => new RestockFrequencyDto
                {
                    ProductId =(int) g.Key.ProductId,
                    ShelfId =(int) g.Key.ShelfId,
                    AlertCount = g.Count(),
                    TotalDays = EF.Functions.DateDiffDay(g.Min(r => r.CreatedAt), g.Max(r => r.CreatedAt)),
                    AvgRestockFrequencyDays = Math.Round(
                        g.Count() == 0 ? 0 : (double)EF.Functions.DateDiffDay(g.Min(r => r.CreatedAt), g.Max(r => r.CreatedAt)) / g.Count(), 2)
                })
                .ToListAsync();

            return Ok(result);
        }
    }
}
