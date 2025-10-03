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
    [Authorize]
    public class StockRequestController : ControllerBase
    {
        private readonly IStockRequest _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;

        public StockRequestController(IStockRequest repository, IMapper mapper, ShelfSenseDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        // 🔍 Get all stock requests
        [Authorize(Roles = "manager,warehouse,staff")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var requests = await _repository.GetAll().ToListAsync();
                var response = _mapper.Map<List<StockRequestResponse>>(requests);
                return Ok(new { message = "Stock requests retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving stock requests.", details = ex.Message });
            }
        }

        // 🔍 Get request by ID
        [Authorize(Roles = "manager,warehouse,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var request = await _repository.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                var response = _mapper.Map<StockRequestResponse>(request);
                return Ok(new { message = "Stock request retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving stock request {id}.", details = ex.Message });
            }
        }

        // 📝 Create new stock request and log inventory report
        [Authorize(Roles = "manager,staff")]
        [Authorize(Roles = "manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockRequestCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

                var entity = _mapper.Map<StockRequest>(request);
                entity.RequestDate = DateTime.UtcNow;
                entity.DeliveryStatus = "requested";

                await _repository.AddAsync(entity);

                // Find a shelf associated with the product in that store to log the report against
                var shelfId = await _context.ProductShelves
                    .Where(ps => ps.ProductId == request.ProductId && ps.Shelf.StoreId == request.StoreId)
                    .Select(ps => ps.ShelfId)
                    .FirstOrDefaultAsync();

                if (shelfId != 0)
                    // Log the creation of the request (QuantityRestocked = null)
                    await LogInventoryReportAsync(request.ProductId, shelfId, null, false);

                var response = _mapper.Map<StockRequestResponse>(entity);
                return CreatedAtAction(nameof(GetById), new { id = response.RequestId }, new
                {
                    message = "Stock request created successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating stock request.", details = ex.Message });
            }
        }

        // ✏️ Update delivery status (general purpose)
        [Authorize(Roles = "manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] StockRequestCreateRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                existing.DeliveryStatus = request.DeliveryStatus;
                existing.EstimatedTimeOfArrival = request.EstimatedTimeOfArrival;

                await _repository.UpdateAsync(existing);
                return Ok(new { message = "Stock request updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating stock request {id}.", details = ex.Message });
            }
        }


        // 🚚 NEW: Mark stock request as in transit
        [Authorize(Roles = "warehouse")]
        [HttpPost("{id}/mark-in-transit")]
        public async Task<IActionResult> MarkAsInTransit(long id)
        {
            try
            {
                var request = await _repository.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered" || request.DeliveryStatus == "cancelled")
                    return BadRequest(new { message = $"Cannot set status to 'in_transit' for a request that is already '{request.DeliveryStatus}'." });

                request.DeliveryStatus = "in_transit";
                await _repository.UpdateAsync(request);

                // Find and update the active alert status
                var shelf = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .FirstOrDefaultAsync(ps => ps.ProductId == request.ProductId && ps.Shelf.StoreId == request.StoreId);

                if (shelf != null)
                {
                    var activeAlert = await _context.ReplenishmentAlerts
                        .Where(a => a.ProductId == request.ProductId && a.ShelfId == shelf.ShelfId && a.Status != "closed")
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (activeAlert != null)
                    {
                        // 🌟 Requirement: Update the active alert's status to show it's in transit 🌟
                        activeAlert.Status = "Order in transit";
                        activeAlert.FulfillmentNote = $"Stock requested on {request.RequestDate:yyyy-MM-dd} is now IN TRANSIT.";
                        _context.ReplenishmentAlerts.Update(activeAlert);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Stock request marked as 'in_transit'. Alert updated for store visibility." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error marking stock request {id} as in transit.", details = ex.Message });
            }
        }

        // ❌ NEW: Mark stock request as cancelled and archive it
        // ❌ NEW: Mark stock request as cancelled and archive it
        [Authorize(Roles = "warehouse")]
        [HttpPost("{id}/mark-cancelled")]
        public async Task<IActionResult> MarkAsCancelled(long id, [FromQuery] string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // 🌟 FIX: Declare activeAlert here so it's accessible outside the 'if (shelf != null)' block.
            ReplenishmentAlert? activeAlert = null;

            try
            {
                var request = await _repository.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered" || request.DeliveryStatus == "cancelled")
                    return BadRequest(new { message = $"Cannot cancel a request that is already '{request.DeliveryStatus}'." });

                request.DeliveryStatus = "cancelled";

                // --- 1. Update the associated active Alert ---
                var shelf = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .FirstOrDefaultAsync(ps => ps.ProductId == request.ProductId && ps.Shelf.StoreId == request.StoreId);

                if (shelf != null)
                {
                    // Assign the result to the variable declared outside the block.
                    activeAlert = await _context.ReplenishmentAlerts
                        .Where(a => a.ProductId == request.ProductId && a.ShelfId == shelf.ShelfId && a.Status != "closed")
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (activeAlert != null)
                    {
                        // 🌟 FIX: Shorten status string further to "WH Cancelled Request" (20 chars) to prevent database truncation error
                        activeAlert.Status = "WH Cancelled Request";
                        activeAlert.FulfillmentNote = $"Stock request cancelled on {DateTime.UtcNow:yyyy-MM-dd HH:mm}. Reason: {reason ?? "Not specified"}";
                        _context.ReplenishmentAlerts.Update(activeAlert);
                    }
                }

                // --- 2. Archive the cancelled Stock Request ---
                var cancelledRecord = new CancelledStockRequest
                {
                    OriginalRequestId = request.RequestId,
                    ProductId = request.ProductId,
                    StoreId = request.StoreId,
                    Quantity = request.Quantity,
                    RequestDate = request.RequestDate,
                    CancelledAt = DateTime.UtcNow,
                    CancellationReason = reason ?? "Not specified",
                    AlertId = (shelf != null && activeAlert != null) ? (int?)activeAlert.AlertId : null,
                };

                _context.CancelledStockRequests.Add(cancelledRecord);

                // --- 3. Delete the request from the active table ---
                _context.StockRequests.Remove(request);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Stock request successfully cancelled and archived. Active alert status updated." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Error marking stock request {id} as cancelled.", details = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        // ✅ Mark as delivered and update shelf quantity (THE CORE LOGIC)
        [Authorize(Roles = "warehouse")]
        [HttpPost("{id}/mark-delivered")]
        public async Task<IActionResult> MarkAsDelivered(long id)
        {
            try
            {
                var request = await _repository.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                if (request.DeliveryStatus == "delivered")
                    return BadRequest(new { message = "Stock already marked as delivered." });

                request.DeliveryStatus = "delivered";
                request.DeliveredAt = DateTime.UtcNow;

                // ✅ Find matching shelf
                var shelf = await _context.ProductShelves
                    .Include(ps => ps.Shelf)
                    .FirstOrDefaultAsync(ps =>
                        ps.ProductId == request.ProductId &&
                        ps.Shelf.StoreId == request.StoreId);

                if (shelf == null)
                {
                    // Note: If the shelf is missing, we shouldn't proceed with restock, but we will update the request status.
                    await _repository.UpdateAsync(request);
                    return NotFound(new
                    {
                        message = $"No matching product-shelf pair found for Product ID {request.ProductId} in Store ID {request.StoreId}. Request status updated but shelf NOT restocked.",
                        requestId = request.RequestId
                    });
                }

                // --- 1. Update shelf quantity ---
                shelf.Quantity += request.Quantity;
                _context.ProductShelves.Update(shelf);

                // --- 2. Log inventory report ---
                await LogInventoryReportAsync(request.ProductId, shelf.ShelfId, request.Quantity, false);

                // No save here yet, wait for the transaction block below

                ReplenishmentAlert? fulfilledAlert = null;
                ReplenishmentAlert? nextAlert = null;
                int? alertCompletedId = null;
                int? alertReRequestedId = null;
                DateTime closedAt = DateTime.UtcNow;


                // --- 3. Archive and DELETE the Alert that triggered this request ---
                // Find the alert that was marked "completed" when the StockRequest was initially created.
                // If the old flow marked it "open", we will complete it now, then archive/delete.
                fulfilledAlert = await _context.ReplenishmentAlerts
                    .Where(a => a.ProductId == request.ProductId &&
                                 a.ShelfId == shelf.ShelfId &&
                                 (a.Status == "open" || a.Status == "completed")) // Check both statuses
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (fulfilledAlert != null)
                {
                    // Ensure status is complete before archiving
                    if (fulfilledAlert.Status == "open")
                    {
                        fulfilledAlert.Status = "completed";
                        fulfilledAlert.FulfillmentNote = $"Order received and restocked on {closedAt:yyyy-MM-dd HH:mm}";
                        _context.ReplenishmentAlerts.Update(fulfilledAlert);
                    }

                    // 🌟 ARCHIVE THE ALERT 🌟
                    var closedAlert = new ClosedReplenishmentAlert
                    {
                        OriginalAlertId = fulfilledAlert.AlertId,
                        ProductId = fulfilledAlert.ProductId,
                        ShelfId = fulfilledAlert.ShelfId,
                        PredictedDepletionDate = fulfilledAlert.PredictedDepletionDate,
                        UrgencyLevel = fulfilledAlert.UrgencyLevel,
                        Status = fulfilledAlert.Status,
                        FulfillmentNote = fulfilledAlert.FulfillmentNote,
                        CreatedAt = fulfilledAlert.CreatedAt,
                        ClosedAt = closedAt // Use the common timestamp
                    };
                    _context.ClosedReplenishmentAlerts.Add(closedAlert);

                    // 🌟 DELETE THE ALERT from the active table 🌟
                    _context.ReplenishmentAlerts.Remove(fulfilledAlert);
                    alertCompletedId = (int)fulfilledAlert.AlertId;
                }


                // --- 4. Archive delivered stock request ---
                var deliveredRecord = new DeliveredStockRequest
                {
                    OriginalRequestId = request.RequestId,
                    ProductId = request.ProductId,
                    StoreId = request.StoreId,
                    Quantity = request.Quantity,
                    DeliveredAt = request.DeliveredAt.Value,
                    AlertId = alertCompletedId,
                    Notes = "Delivered and shelf updated"
                };

                _context.DeliveredStockRequests.Add(deliveredRecord);
                _context.StockRequests.Remove(request); // Remove from active table

                // Persist all changes: shelf update, alert archive/delete, and request archive/delete
                await _context.SaveChangesAsync();


                // --- 5. Re-trigger Next Cycle (New Alert check) ---

                // Find the next *oldest* alert that is still open for this product/shelf
                nextAlert = await _context.ReplenishmentAlerts
                    .Where(a => a.ProductId == request.ProductId &&
                                 a.ShelfId == shelf.ShelfId &&
                                 a.Status == "open")
                    .OrderBy(a => a.CreatedAt) // Oldest first
                    .FirstOrDefaultAsync();

                if (nextAlert != null)
                {
                    bool requestExists = await _context.StockRequests.AnyAsync(r =>
                        r.ProductId == request.ProductId &&
                        r.StoreId == shelf.Shelf.StoreId &&
                        r.DeliveryStatus == "requested");

                    if (!requestExists)
                    {
                        var quantityNeeded = shelf.Shelf.Capacity - shelf.Quantity;

                        if (quantityNeeded > 0)
                        {
                            var newRequest = new StockRequest
                            {
                                ProductId = request.ProductId,
                                StoreId = shelf.Shelf.StoreId,
                                Quantity = quantityNeeded,
                                RequestDate = DateTime.UtcNow,
                                DeliveryStatus = "requested"
                            };

                            _context.StockRequests.Add(newRequest);

                            // Immediately complete and archive this new alert that triggered the new request
                            nextAlert.Status = "completed";
                            nextAlert.FulfillmentNote = $"Auto-requested immediately after prior delivery due to continued low stock on {DateTime.UtcNow:yyyy-MM-dd HH:mm}";

                            // 🌟 ARCHIVE AND DELETE THE NEWLY COMPLETED ALERT 🌟
                            var closedNextAlert = new ClosedReplenishmentAlert
                            {
                                OriginalAlertId = nextAlert.AlertId,
                                ProductId = nextAlert.ProductId,
                                ShelfId = nextAlert.ShelfId,
                                PredictedDepletionDate = nextAlert.PredictedDepletionDate,
                                UrgencyLevel = nextAlert.UrgencyLevel,
                                Status = "completed",
                                FulfillmentNote = nextAlert.FulfillmentNote,
                                CreatedAt = nextAlert.CreatedAt,
                                ClosedAt = DateTime.UtcNow // New closure time
                            };
                            _context.ClosedReplenishmentAlerts.Add(closedNextAlert);
                            _context.ReplenishmentAlerts.Remove(nextAlert); // DELETE from active table
                            alertReRequestedId = (int)nextAlert.AlertId;

                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return Ok(new
                {
                    message = "Stock request fulfilled. Shelf updated. Alert archived and deleted. Next cycle processed.",
                    requestId = request.RequestId,
                    productId = request.ProductId,
                    storeId = request.StoreId,
                    deliveredAt = request.DeliveredAt,
                    alertArchived = alertCompletedId,
                    alertReRequestedAndArchived = alertReRequestedId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Error marking stock request {id} as delivered.",
                    details = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }


        // 🗑️ Delete request
        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Stock request ID {id} not found." });

                await _repository.DeleteAsync(id);
                return Ok(new { message = $"Stock request ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting stock request {id}.", details = ex.Message });
            }
        }

        // 🔧 Internal method to log inventory report
        private async Task LogInventoryReportAsync(long productId, long shelfId, int? quantityRestocked, bool alertTriggered)
        {
            // Note: This method should ideally check if a report already exists for the current UTC day and UPDATE it, 
            // rather than only creating a new one if none exists.
            try
            {
                var shelfState = await _context.ProductShelves
                    .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.ShelfId == shelfId);

                if (shelfState == null) return;

                // Find the report for today's date (local date comparison is fine here)
                var report = await _context.InventoryReports
                    .FirstOrDefaultAsync(r =>
                        r.ProductId == productId &&
                        r.ShelfId == shelfId &&
                        r.ReportDate == DateTime.Today);

                if (report == null)
                {
                    // Create new report entry
                    report = new InventoryReport
                    {
                        ProductId = productId,
                        ShelfId = shelfId,
                        ReportDate = DateTime.Today,
                        QuantityOnShelf = shelfState.Quantity, // Current quantity
                        QuantityRestocked = quantityRestocked,
                        AlertTriggered = alertTriggered,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.InventoryReports.Add(report);
                }
                else
                {
                    // Update existing report entry
                    report.QuantityOnShelf = shelfState.Quantity; // Update to the new quantity after restock
                    report.QuantityRestocked = (report.QuantityRestocked ?? 0) + (quantityRestocked ?? 0);
                    report.AlertTriggered = report.AlertTriggered || alertTriggered;
                    // Note: We don't update CreatedAt/ReportDate since this is a daily aggregate
                    _context.InventoryReports.Update(report);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging inventory report: {ex.Message}");
            }
        }
    }
}