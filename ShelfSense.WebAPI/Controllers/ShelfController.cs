using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
//using  ShelfSense.Application.DTOs;


namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelfController : ControllerBase
    {
        private readonly IShelfRepository _repository;
        private readonly IMapper _mapper;

        public ShelfController(IShelfRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var shelves = _repository.GetAll().ToList();
            var response = _mapper.Map<List<ShelfResponse>>(shelves);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var shelf = await _repository.GetByIdAsync(id);
            if (shelf == null)
                return NotFound(new { message = $"Shelf with ID {id} not found." });

            var response = _mapper.Map<ShelfResponse>(shelf);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShelfCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var shelf = _mapper.Map<Shelf>(request);

            try
            {
                await _repository.AddAsync(shelf);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Shelves_ShelfCode") == true)
            {
                return Conflict(new { message = $"Shelf code '{request.ShelfCode}' already exists." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Shelves_Stores_StoreId") == true)
            {
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
            }

            var response = _mapper.Map<ShelfResponse>(shelf);
            return CreatedAtAction(nameof(GetById), new { id = response.ShelfId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ShelfCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Shelf with ID {id} not found." });

            existing.ShelfCode = request.ShelfCode;
            existing.StoreId = request.StoreId;
            existing.LocationDescription = request.LocationDescription;

            try
            {
                await _repository.UpdateAsync(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Shelves_ShelfCode") == true)
            {
                return Conflict(new { message = $"Shelf code '{request.ShelfCode}' already exists." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Shelves_Stores_StoreId") == true)
            {
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Shelf with ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
