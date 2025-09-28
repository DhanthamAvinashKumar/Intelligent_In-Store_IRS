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
    public class StockRequestController : ControllerBase
    {
        private readonly IStockRequest _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public StockRequestController(
            IStockRequest repository,
            IMapper mapper,
            ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔓 Accessible to manager and staff
        [Authorize(Roles = "manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            var requests = _repository.GetAll().ToList();
            var response = _mapper.Map<List<StockRequestResponse>>(requests);
            return Ok(new { message = "Stock requests retrieved successfully.", data = response });
        }

        [Authorize(Roles = "manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return NotFound(new { message = $"StockRequest ID {id} not found." });

            var response = _mapper.Map<StockRequestResponse>(request);
            return Ok(new { message = "Stock request retrieved successfully.", data = response });
        }

        // 🔐 Staff can create requests
        [Authorize(Roles = "staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockRequestCreateRequest request)
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

            if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            var entity = _mapper.Map<StockRequest>(request);
            await _repository.AddAsync(entity);

            var response = _mapper.Map<StockRequestResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.RequestId }, new
            {
                message = "Stock request created successfully.",
                data = response
            });
        }

        // 🔐 Manager-only
        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] StockRequestCreateRequest request)
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
                return NotFound(new { message = $"StockRequest ID {id} not found." });

            if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            _mapper.Map(request, existing);
            await _repository.UpdateAsync(existing);

            return Ok(new { message = $"Stock request ID {id} updated successfully." });
        }

        // 🔐 Manager-only with confirmation
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm = false)
        {
            if (!confirm)
                return BadRequest(new
                {
                    message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                });

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"StockRequest ID {id} not found." });

            await _repository.DeleteAsync(id);
            return Ok(new { message = $"Stock request ID {id} deleted successfully." });
        }
    }
}
