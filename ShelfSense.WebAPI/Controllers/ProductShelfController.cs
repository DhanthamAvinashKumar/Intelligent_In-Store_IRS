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
    public class ProductShelfController : ControllerBase
    {
        private readonly IProductShelfRepository _repository;
        private readonly ShelfSenseDbContext _dbContext;
        private readonly IMapper _mapper;

        public ProductShelfController(IProductShelfRepository repository, ShelfSenseDbContext dbContext, IMapper mapper)
        {
            _repository = repository;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var items = _repository.GetAll().ToList();
            var response = _mapper.Map<List<ProductShelfResponse>>(items);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return NotFound(new { message = $"ProductShelf with ID {id} not found." });

            var response = _mapper.Map<ProductShelfResponse>(item);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductShelfCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _dbContext.Products.FindAsync(request.ProductId);
            var shelf = await _dbContext.Shelves.FindAsync(request.ShelfId);

            if (product == null)
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            if (shelf == null)
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

            if (product.CategoryId != shelf.CategoryId)
                return BadRequest(new { message = "Product and shelf categories must match." });

            var entity = _mapper.Map<ProductShelf>(request);

            try
            {
                await _repository.AddAsync(entity);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
            {
                return Conflict(new { message = "This product is already assigned to this shelf." });
            }

            var response = _mapper.Map<ProductShelfResponse>(entity);
            return CreatedAtAction(nameof(GetById), new { id = response.ProductShelfId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProductShelfCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"ProductShelf with ID {id} not found." });

            var product = await _dbContext.Products.FindAsync(request.ProductId);
            var shelf = await _dbContext.Shelves.FindAsync(request.ShelfId);

            if (product == null)
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

            if (shelf == null)
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

            if (product.CategoryId != shelf.CategoryId)
                return BadRequest(new { message = "Product and shelf categories must match." });

            existing.ProductId = request.ProductId;
            existing.ShelfId = request.ShelfId;
            existing.Quantity = request.Quantity;

            try
            {
                await _repository.UpdateAsync(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
            {
                return Conflict(new { message = "This product is already assigned to this shelf." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"ProductShelf with ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
