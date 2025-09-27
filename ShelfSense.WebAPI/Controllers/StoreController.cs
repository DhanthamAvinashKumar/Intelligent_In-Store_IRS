using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Application.DTOs;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreRepository _repository;
        private readonly IMapper _mapper;

        public StoreController(IStoreRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var stores = _repository.GetAll().ToList();
            var response = _mapper.Map<List<StoreResponse>>(stores);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var store = await _repository.GetByIdAsync(id);
            if (store == null)
                return NotFound(new { message = $"Store with ID {id} not found." });

            var response = _mapper.Map<StoreResponse>(store);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var store = _mapper.Map<Store>(request);

            try
            {
                await _repository.AddAsync(store);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Stores_StoreName") == true)
            {
                return Conflict(new { message = $"Store name '{request.StoreName}' already exists." });
            }

            var response = _mapper.Map<StoreResponse>(store);
            return CreatedAtAction(nameof(GetById), new { id = response.StoreId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] StoreCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Store with ID {id} not found." });

            existing.StoreName = request.StoreName;
            existing.Address = request.Address;
            existing.City = request.City;
            existing.State = request.State;
            existing.PostalCode = request.PostalCode;

            try
            {
                await _repository.UpdateAsync(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Stores_StoreName") == true)
            {
                return Conflict(new { message = $"Store name '{request.StoreName}' already exists." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Store with ID {id} not found." });

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
