using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductShelfController : ControllerBase
    {
        private readonly IProductShelfRepository _repository;
        private readonly IMapper _mapper;

        public ProductShelfController(IProductShelfRepository repository, IMapper mapper)
        {
            _repository = repository;
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

            var entity = _mapper.Map<ProductShelf>(request);

            try
            {
                await _repository.AddAsync(entity);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
            {
                return Conflict(new { message = "This product is already assigned to this shelf." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ProductShelves_Products_ProductId") == true)
            {
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ProductShelves_Shelves_ShelfId") == true)
            {
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });
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
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ProductShelves_Products_ProductId") == true)
            {
                return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ProductShelves_Shelves_ShelfId") == true)
            {
                return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });
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
