using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilizationController : ControllerBase
    {
        private readonly ShelfSenseDbContext _context;

        public UtilizationController(ShelfSenseDbContext context)
        {
            _context = context;
        }

        // 🔐 Manager-only access
        [Authorize(Roles = "manager")]
        [HttpGet("low-utilization-with-sales")]
        public async Task<IActionResult> GetLowUtilizationWithSales()
        {
            var utilizationData = await _context.ProductShelves
                .Join(_context.Shelves,
                    ps => ps.ShelfId,
                    s => s.ShelfId,
                    (ps, s) => new
                    {
                        ps.ProductId,
                        ps.ShelfId,
                        ps.Quantity,
                        s.Capacity,
                        UtilizationPercent = Math.Round(ps.Quantity * 100.0 / s.Capacity, 2)
                    })
                .Where(x => x.UtilizationPercent < 50)
                .ToListAsync();

            var enriched = new List<ShelfUtilizationDto>();

            foreach (var entry in utilizationData)
            {
                var recentSales = await _context.SalesHistories
                    .Where(sh => sh.ProductId == entry.ProductId && sh.SaleTime >= DateTime.Now.AddDays(-7))
                    .ToListAsync();

                enriched.Add(new ShelfUtilizationDto
                {
                    ProductId = entry.ProductId,
                    ShelfId = entry.ShelfId,
                    Quantity = entry.Quantity,
                    Capacity = entry.Capacity,
                    UtilizationPercent = entry.UtilizationPercent,
                    SalesCountLast7Days = recentSales.Count,
                    LastSaleTime = recentSales.Max(sh => (DateTime?)sh.SaleTime)
                });
            }

            var filtered = enriched
                .Where(e => e.SalesCountLast7Days >= 1)
                .OrderBy(e => e.UtilizationPercent)
                .ToList();

            // Debug output
            Console.WriteLine($"Total shelves evaluated: {utilizationData.Count}");
            Console.WriteLine($"Enriched entries: {enriched.Count}");
            Console.WriteLine($"Filtered entries: {filtered.Count}");

            return Ok(new
            {
                message = "Low-utilization shelves with recent sales retrieved successfully.",
                data = filtered
            });
        }
    }
}
