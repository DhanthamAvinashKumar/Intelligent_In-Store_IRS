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

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var tasks = _repository.GetAll().ToList();
                var response = _mapper.Map<List<RestockTaskResponse>>(tasks);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving restock tasks.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var task = await _repository.GetByIdAsync(id);
                if (task == null)
                    return NotFound(new { message = $"Task ID {id} not found." });

                var response = _mapper.Map<RestockTaskResponse>(task);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving the restock task.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RestockTaskCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validate foreign keys
                var alertExists = await _context.ReplenishmentAlerts.AnyAsync(a => a.AlertId == request.AlertId);
                if (!alertExists)
                    return BadRequest(new { message = $"Alert ID '{request.AlertId}' does not exist." });

                var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
                if (!productExists)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                var shelfExists = await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId);
                if (!shelfExists)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                var staffExists = await _context.Staffs.AnyAsync(s => s.StaffId == request.AssignedTo);
                if (!staffExists)
                    return BadRequest(new { message = $"Staff ID '{request.AssignedTo}' does not exist." });

                var entity = _mapper.Map<RestockTask>(request);
                await _repository.AddAsync(entity);

                var response = _mapper.Map<RestockTaskResponse>(entity);
                return CreatedAtAction(nameof(GetById), new { id = response.TaskId }, response);
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Database error while creating restock task.", details = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while creating restock task.", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RestockTaskCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task ID {id} not found." });

                // Validate foreign keys
                var alertExists = await _context.ReplenishmentAlerts.AnyAsync(a => a.AlertId == request.AlertId);
                if (!alertExists)
                    return BadRequest(new { message = $"Alert ID '{request.AlertId}' does not exist." });

                var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
                if (!productExists)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                var shelfExists = await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId);
                if (!shelfExists)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                var staffExists = await _context.Staffs.AnyAsync(s => s.StaffId == request.AssignedTo);
                if (!staffExists)
                    return BadRequest(new { message = $"Staff ID '{request.AssignedTo}' does not exist." });

                _mapper.Map(request, existing);
                await _repository.UpdateAsync(existing);
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Database error while updating restock task.", details = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while updating restock task.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            long id,
            [FromHeader(Name = "X-Confirm-Delete")] bool confirm = false)
        {
            if (!confirm)
                return BadRequest(new
                {
                    message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                });

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Task ID {id} not found." });

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting restock task.", details = ex.Message });
            }
        }
    }
}
