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
            var result = await _context.ProductShelves
                .Join(_context.Shelves, ps => ps.ShelfId, s => s.ShelfId, (ps, s) => new
                {
                    ps.ProductId,
                    ps.ShelfId,
                    ps.Quantity,
                    s.Capacity,
                    UtilizationPercent = Math.Round(ps.Quantity * 100.0 / s.Capacity, 2)
                })
                .Where(x => x.UtilizationPercent < 20)
                .ToListAsync();

            var enriched = new List<ShelfUtilizationDto>();

            foreach (var x in result)
            {
                var sales = await _context.SalesHistories
                    .Where(sh => sh.ProductId == x.ProductId && sh.SaleTime >= DateTime.Now.AddDays(-7))
                    .ToListAsync();

                enriched.Add(new ShelfUtilizationDto
                {
                    ProductId = x.ProductId,
                    ShelfId = x.ShelfId,
                    Quantity = x.Quantity,
                    Capacity = x.Capacity,
                    UtilizationPercent = x.UtilizationPercent,
                    SalesCountLast7Days = sales.Count,
                    LastSaleTime = sales.Max(sh => (DateTime?)sh.SaleTime)
                });
            }

            var filtered = enriched.Where(e => e.SalesCountLast7Days > 1).ToList();

            return Ok(new
            {
                message = "Low-utilization shelves with recent sales retrieved successfully.",
                data = filtered
            });
        }
    }
}
