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

        // 🔓 Accessible to all authenticated users
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var items = _repository.GetAll().ToList();
                var response = _mapper.Map<List<ProductShelfResponse>>(items);
                return Ok(new { message = "ProductShelves retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving ProductShelves.", details = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(id);
                if (item == null)
                    return NotFound(new { message = $"ProductShelf with ID {id} not found." });

                var response = _mapper.Map<ProductShelfResponse>(item);
                return Ok(new { message = "ProductShelf retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving ProductShelf {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductShelfCreateRequest request)
        {
            try
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
                return CreatedAtAction(nameof(GetById), new { id = response.ProductShelfId }, new
                {
                    message = "Product assigned to shelf successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating ProductShelf.", details = ex.Message });
            }

        }

        // 🔐 Manager-only
        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProductShelfCreateRequest request)
        {
            try
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

                return Ok(new { message = $"ProductShelf with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating ProductShelf {id}.", details = ex.Message });
            }

        }

        // 🔐 Manager-only with confirmation and constraint handling
        [Authorize(Roles = "manager")]
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
                    return NotFound(new { message = $"ProductShelf with ID {id} not found." });

                try
                {
                    await _repository.DeleteAsync(id);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    return Conflict(new
                    {
                        message = $"Cannot delete ProductShelf ID {id} because it is referenced in other records."
                    });
                }

                return Ok(new { message = $"ProductShelf with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Unexpected error while deleting ProductShelf {id}.",
                    details = ex.Message
                });
            }
        }


        // 🔍 Predict stock depletion and generate alerts
        [Authorize]
        [HttpGet("predict-depletion")]
        public async Task<IActionResult> PredictDepletion()
        {
            try
            {
                var velocityData = await (
                    from sale in _dbContext.SalesHistories
                    group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
                    select new
                    {
                        daily.Key.ProductId,
                        DailySales = daily.Sum(x => x.Quantity)
                    }
                ).ToListAsync();

                var salesVelocity = velocityData
                    .GroupBy(x => x.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        SalesVelocity = g.Average(x => x.DailySales)
                    }).ToDictionary(x => x.ProductId, x => x.SalesVelocity);

                var shelves = await _dbContext.ProductShelves.ToListAsync();
                var alertsToInsert = new List<ReplenishmentAlert>();
                var predictions = new List<StockDepletionPredictionDto>();

                foreach (var ps in shelves)
                {
                    try
                    {
                        if (!salesVelocity.ContainsKey(ps.ProductId)) continue;

                        var velocity = salesVelocity[ps.ProductId];
                        var daysToDepletion = velocity > 0 ? Math.Round(ps.Quantity / velocity, 2) : double.MaxValue;
                        DateTime? expectedDate = velocity > 0
                            ? DateTime.Today.AddDays(Math.Round(ps.Quantity / velocity))
                            : null;
                        var isLowStock = daysToDepletion < 5;

                        predictions.Add(new StockDepletionPredictionDto
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            Quantity = ps.Quantity,
                            SalesVelocity = velocity,
                            DaysToDepletion = daysToDepletion,
                            ExpectedDepletionDate = expectedDate,
                            IsLowStock = isLowStock
                        });

                        if (isLowStock)
                        {
                            var urgency = daysToDepletion switch
                            {
                                <= 1 => "critical",
                                <= 2 => "high",
                                <= 4 => "medium",
                                _ => "low"
                            };

                            var exists = await _dbContext.ReplenishmentAlerts.AnyAsync(a =>
                                a.ProductId == ps.ProductId &&
                                a.ShelfId == ps.ShelfId &&
                                a.Status == "open");

                            if (!exists)
                            {
                                alertsToInsert.Add(new ReplenishmentAlert
                                {
                                    ProductId = ps.ProductId,
                                    ShelfId = ps.ShelfId,
                                    PredictedDepletionDate = expectedDate ?? DateTime.Today,
                                    UrgencyLevel = urgency,
                                    Status = "open",
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        // If one shelf fails, skip it but continue with others
                        predictions.Add(new StockDepletionPredictionDto
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            Quantity = ps.Quantity,
                            SalesVelocity = 0,
                            DaysToDepletion = double.MaxValue,
                            ExpectedDepletionDate = null,
                            IsLowStock = false
                        });
                        // Optionally log innerEx here
                    }
                }

                if (alertsToInsert.Any())
                {
                    await _dbContext.ReplenishmentAlerts.AddRangeAsync(alertsToInsert);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Stock depletion predictions generated successfully.",
                    data = predictions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error generating stock depletion predictions.",
                    details = ex.Message
                });
            }
        }

    }
}
