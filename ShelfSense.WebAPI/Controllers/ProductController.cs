using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using static ShelfSense.Application.DTOs.ProductDto;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;

        public ProductController(IProductRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var products = _repository.GetAll().ToList();
            var response = _mapper.Map<List<ProductResponse>>(products);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            var response = _mapper.Map<ProductResponse>(product);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = _mapper.Map<Product>(request);

            try
            {
                await _repository.AddAsync(product);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
            {
                return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
            }

            var response = _mapper.Map<ProductResponse>(product);
            return CreatedAtAction(nameof(GetById), new { id = response.ProductId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProductCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            existing.StockKeepingUnit = request.StockKeepingUnit;
            existing.ProductName = request.ProductName;
            existing.CategoryId = request.CategoryId;
            existing.PackageSize = request.PackageSize;
            existing.Unit = request.Unit;

            try
            {
                await _repository.UpdateAsync(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
            {
                return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}