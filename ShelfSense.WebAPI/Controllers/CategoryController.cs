using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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

        // 🔓 Accessible to any authenticated user
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var categories = _repository.GetAll().ToList();
                var response = _mapper.Map<List<CategoryResponse>>(categories);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving categories.", detail = ex.Message });
            }
        }

        // 🔓 Accessible to any authenticated user
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var category = await _repository.GetByIdAsync(id);
                if (category == null)
                    return NotFound(new { message = $"Category with ID {id} not found." });

                var response = _mapper.Map<CategoryResponse>(category);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving category {id}.", detail = ex.Message });
            }
        }

        // 🔐 Restricted to manager role
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateRequest request)
        {
            try
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

                return CreatedAtAction(nameof(GetById), new { id = response.CategoryId }, new
                {
                    message = $"Category '{response.CategoryName}' created successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the category.", detail = ex.Message });
            }
        }

        // 🔐 Restricted to manager role
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CategoryCreateRequest request)
        {
            try
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

                return Ok(new { message = $"Category with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while updating category {id}.", detail = ex.Message });
            }
        }

        // 🔐 Restricted to manager role
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            try
            {
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found." });

                try
                {
                    await _repository.DeleteAsync(id);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    return Conflict(new
                    {
                        message = $"Cannot delete Category ID {id} because it is referenced in other records (e.g., Products or InventoryReports)."
                    });
                }

                return Ok(new { message = $"Category with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while deleting category {id}.", detail = ex.Message });
            }
        }
    }
}
