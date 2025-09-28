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
    public class SalesHistoryController : ControllerBase
    {
        private readonly ISalesHistory _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public SalesHistoryController(
            ISalesHistory repository,
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
            var sales = _repository.GetAll().ToList();
            var response = _mapper.Map<List<SalesHistoryResponse>>(sales);
            return Ok(new { message = "Sales history retrieved successfully.", data = response });
        }

        [Authorize(Roles = "manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var sale = await _repository.GetByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = $"Sale ID {id} not found." });

            var response = _mapper.Map<SalesHistoryResponse>(sale);
            return Ok(new { message = "Sales record retrieved successfully.", data = response });
        }

        // 🔐 Manager-only
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesHistoryCreateRequest request)
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

            var entity = _mapper.Map<SalesHistory>(request);
            await _repository.AddAsync(entity);

            var response = _mapper.Map<SalesHistoryResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.SaleId }, new
            {
                message = "Sales record created successfully.",
                data = response
            });
        }

        // 🔐 Manager-only
        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] SalesHistoryCreateRequest request)
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
                return NotFound(new { message = $"Sale ID {id} not found." });

            if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            _mapper.Map(request, existing);
            await _repository.UpdateAsync(existing);

            return Ok(new { message = $"Sales record ID {id} updated successfully." });
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
                return NotFound(new { message = $"Sale ID {id} not found." });

            await _repository.DeleteAsync(id);
            return Ok(new { message = $"Sales record ID {id} deleted successfully." });
        }
    }
}
