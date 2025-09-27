using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;

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

        [HttpGet]
        public IActionResult GetAll()
        {
            var sales = _repository.GetAll().ToList();
            var response = _mapper.Map<List<SalesHistoryResponse>>(sales);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var sale = await _repository.GetByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = $"Sale ID {id} not found." });

            var response = _mapper.Map<SalesHistoryResponse>(sale);
            return Ok(response);
        }

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

            var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId);
            if (!storeExists)
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
            if (!productExists)
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            var entity = _mapper.Map<SalesHistory>(request);
            await _repository.AddAsync(entity);

            var response = _mapper.Map<SalesHistoryResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.SaleId }, response);
        }

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

            var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId);
            if (!storeExists)
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == request.ProductId);
            if (!productExists)
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            _mapper.Map(request, existing);
            await _repository.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Sale ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
