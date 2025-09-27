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
    public class ReplenishmentAlertController : ControllerBase
    {
        private readonly IReplenishmentAlert _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public ReplenishmentAlertController(
            IReplenishmentAlert repository,
            IMapper mapper,
            ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var alerts = _repository.GetAll().ToList();
            var response = _mapper.Map<List<ReplenishmentAlertResponse>>(alerts);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var alert = await _repository.GetByIdAsync(id);
            if (alert == null)
                return NotFound(new { message = $"Alert ID {id} not found." });

            var response = _mapper.Map<ReplenishmentAlertResponse>(alert);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReplenishmentAlertCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
                        .ToList()
                });

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
            if (!productExists)
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            var shelfExists = await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId);
            if (!shelfExists)
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

            var entity = _mapper.Map<ReplenishmentAlert>(request);
            await _repository.AddAsync(entity);

            var response = _mapper.Map<ReplenishmentAlertResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.AlertId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ReplenishmentAlertCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
                        .ToList()
                });

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Alert ID {id} not found." });

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Alert ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        // 🔔 NEW: Automated Replenishment Trigger
        [HttpPost("check")]
        public async Task<IActionResult> CheckAndTriggerReplenishment()
        {
            var candidates = await _context.ProductShelves
                .Include(ps => ps.Shelf)
                .Where(ps => (ps.Quantity * 100.0 / ps.Shelf.Capacity) < 20)
                .Select(ps => new
                {
                    ps.ProductId,
                    ps.ShelfId,
                    ps.Quantity,
                    Capacity = ps.Shelf.Capacity,
                    UtilizationPercent = (ps.Quantity * 100.0 / ps.Shelf.Capacity),
                    SalesCount = _context.SalesHistories
                        .Count(sh => sh.ProductId == ps.ProductId && sh.SaleTime >= DateTime.Now.AddDays(-7))
                })
                .Where(x => x.SalesCount > 1)
                .ToListAsync();

            int alertsCreated = 0;

            foreach (var item in candidates)
            {
                var alert = new ReplenishmentAlert
                {
                    ProductId = item.ProductId,
                    ShelfId = item.ShelfId,
                    PredictedDepletionDate = DateTime.Now.AddDays(2),
                    UrgencyLevel = "high",
                    Status = "open",
                    CreatedAt = DateTime.Now
                };

                _context.ReplenishmentAlerts.Add(alert);
                await _context.SaveChangesAsync();
                alertsCreated++;

                var storeId = await _context.Shelves
                    .Where(sh => sh.ShelfId == item.ShelfId)
                    .Select(sh => sh.StoreId)
                    .FirstOrDefaultAsync();

                var staff = await _context.Staffs
                    .Where(s => s.StoreId == storeId)
                    .FirstOrDefaultAsync();

                if (staff != null)
                {
                    var task = new RestockTask
                    {
                        AlertId = alert.AlertId,
                        ProductId = item.ProductId,
                        ShelfId = item.ShelfId,
                        AssignedTo = staff.StaffId,
                        Status = "pending",
                        AssignedAt = DateTime.Now
                    };

                    _context.RestockTasks.Add(task);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Replenishment check completed", alertsCreated });
        }
    }
}
