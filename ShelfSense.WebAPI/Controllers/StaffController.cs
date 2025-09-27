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
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public StaffController(IStaffRepository repository, IMapper mapper, ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var staff = _repository.GetAll().ToList();
                var response = _mapper.Map<List<StaffResponse>>(staff);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving staff records.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var staff = await _repository.GetByIdAsync(id);
                if (staff == null)
                    return NotFound(new { message = $"Staff ID {id} not found." });

                var response = _mapper.Map<StaffResponse>(staff);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the staff record.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId);
                if (!storeExists)
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

                var entity = _mapper.Map<Staff>(request);
                await _repository.AddAsync(entity);

                var response = _mapper.Map<StaffResponse>(entity);
                return CreatedAtAction(nameof(GetById), new { id = response.StaffId }, response);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Staff_Email") == true)
            {
                return Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Staff_Store") == true)
            {
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the staff record.", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] StaffCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Staff ID {id} not found." });

                var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId);
                if (!storeExists)
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

                existing.StoreId = request.StoreId;
                existing.Name = request.Name;
                existing.Role = request.Role;
                existing.Email = request.Email;
                existing.PasswordHash = request.PasswordHash;

                await _repository.UpdateAsync(existing);
                return NoContent();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Staff_Email") == true)
            {
                return Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Staff_Store") == true)
            {
                return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the staff record.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Staff ID {id} not found." });

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the staff record.", details = ex.Message });
            }
        }
    }
}
