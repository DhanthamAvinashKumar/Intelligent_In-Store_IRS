using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using static ShelfSense.Application.DTOs.CategoryDto;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _repository;
        private readonly IMapper _mapper;

        public CategoryController(ICategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var categories = _repository.GetAll().ToList();
            var response = _mapper.Map<List<CategoryResponse>>(categories);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { message = $"Category with ID {id} not found." });

            var response = _mapper.Map<CategoryResponse>(category);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = _mapper.Map<Category>(request);

            try
            {
                await _repository.AddAsync(category);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
            {
                return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
            }

            var response = _mapper.Map<CategoryResponse>(category);
            return CreatedAtAction(nameof(GetById), new { id = response.CategoryId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CategoryCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Category with ID {id} not found." });

            existing.CategoryName = request.CategoryName;
            existing.Description = request.Description;

            try
            {
                await _repository.UpdateAsync(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
            {
                return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Category with ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
