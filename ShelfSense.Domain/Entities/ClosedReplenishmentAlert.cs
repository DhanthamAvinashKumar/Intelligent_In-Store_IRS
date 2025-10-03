using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShelfSense.Domain.Entities
{
    // Note: This entity mirrors the ReplenishmentAlert structure for archival purposes
    public class ClosedReplenishmentAlert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ClosedAlertId { get; set; }

        public long OriginalAlertId { get; set; } // Link back to the ID it had in the active table
        public long ProductId { get; set; }
        public long ShelfId { get; set; }

        public DateTime PredictedDepletionDate { get; set; }

        [MaxLength(50)]
        public string UrgencyLevel { get; set; } = "medium";

        [MaxLength(20)]
        public string Status { get; set; } = "completed"; // Should always be completed/closed

        [MaxLength(255)]
        public string? FulfillmentNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ClosedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Optional, but good practice if needed for reporting)
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [ForeignKey("ShelfId")]
        public Shelf Shelf { get; set; }
    }
}