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
            try
            {
                var alerts = _repository.GetAll().ToList();
                var response = _mapper.Map<List<ReplenishmentAlertResponse>>(alerts);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving alerts.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var alert = await _repository.GetByIdAsync(id);
                if (alert == null)
                    return NotFound(new { message = $"Alert ID {id} not found." });

                var response = _mapper.Map<ReplenishmentAlertResponse>(alert);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving alert.", details = ex.Message });
            }
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

            try
            {
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
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Database error while creating alert.", details = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while creating alert.", details = ex.Message });
            }
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

            try
            {
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
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Database error while updating alert.", details = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error while updating alert.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Alert ID {id} not found." });

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting alert.", details = ex.Message });
            }
        }
    }
}
