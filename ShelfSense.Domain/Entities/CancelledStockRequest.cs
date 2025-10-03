using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Domain.Entities
{
    // Used to archive stock requests that have been explicitly cancelled.
    public class CancelledStockRequest
    {
        [Key]
        public long ArchiveId { get; set; }

        public long OriginalRequestId { get; set; }

        [Required]
        public long StoreId { get; set; }

        [Required]
        public long ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime RequestDate { get; set; }

        // Status should be "cancelled" here
        [Required]
        public string DeliveryStatus { get; set; } = "cancelled";

        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        public int? AlertId { get; set; }
        public string CancellationReason { get; set; } = "Warehouse cancelled the request.";
    }
}
