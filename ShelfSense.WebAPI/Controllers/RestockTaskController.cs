 

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
    public class RestockTaskController : ControllerBase
    {
        private readonly IRestockTaskRepository _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public RestockTaskController(IRestockTaskRepository repository, IMapper mapper, ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔍 Get all tasks
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _repository.GetAll().ToListAsync();
            var response = _mapper.Map<List<RestockTaskResponse>>(tasks);
            return Ok(new { message = "Restock tasks retrieved successfully.", data = response });
        }

        // 🔍 Get task by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task == null)
                return NotFound(new { message = $"Restock task ID {id} not found." });

            var response = _mapper.Map<RestockTaskResponse>(task);
            return Ok(response);
        }

        // 📝 Create new restock task and log inventory report
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RestockTaskCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            if (!await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId))
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

            if (!await _context.Staffs.AnyAsync(st => st.StaffId == request.AssignedTo))
                return BadRequest(new { message = $"Staff ID '{request.AssignedTo}' does not exist." });

            var entity = _mapper.Map<RestockTask>(request);
            entity.Status = "pending";
            entity.AssignedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);

            // 🧠 Log to InventoryReport
            await LogInventoryReportAsync(entity.ProductId, entity.ShelfId, null, true);

            var response = _mapper.Map<RestockTaskResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.TaskId }, response);
        }

        // ✅ Mark task as completed and log inventory report
        [Authorize(Roles = "manager")]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteTask(long id, [FromBody] int quantityRestocked)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task == null)
                return NotFound(new { message = $"Restock task ID {id} not found." });

            if (task.Status == "completed")
                return BadRequest(new { message = "Task is already marked as completed." });

            task.Status = "completed";
            task.CompletedAt = DateTime.UtcNow;
            //task.QuantityRestocked = quantityRestocked;

            var shelf = await _context.ProductShelves
                .FirstOrDefaultAsync(ps => ps.ProductId == task.ProductId && ps.ShelfId == task.ShelfId);

            if (shelf != null)
            {
                shelf.Quantity += quantityRestocked;
                _context.ProductShelves.Update(shelf);
            }

            await _repository.UpdateAsync(task);

            // 🧠 Log to InventoryReport
            await LogInventoryReportAsync(task.ProductId, task.ShelfId, quantityRestocked, true);

            return Ok(new { message = "Restock task marked as completed." });
        }

        // 🔧 Internal method to log inventory report
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

        // 🗑️ Delete task
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Restock task ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        
    }
}
