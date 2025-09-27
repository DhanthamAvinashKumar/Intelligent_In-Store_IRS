using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public class ReplenishmentAlertCreateRequest
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public long ShelfId { get; set; }

        [Required]
        public DateTime PredictedDepletionDate { get; set; }

        [Required]
        [RegularExpression("low|medium|high|critical")]
        public string UrgencyLevel { get; set; }

        [RegularExpression("open|acknowledged|resolved")]
        public string Status { get; set; } = "open";
    }

    public class ReplenishmentAlertResponse
    {
        public long AlertId { get; set; }
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public DateTime PredictedDepletionDate { get; set; }
        public string UrgencyLevel { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
