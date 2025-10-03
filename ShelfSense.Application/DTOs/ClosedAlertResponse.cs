namespace ShelfSense.Application.DTOs
{
    public class ClosedAlertResponse
    {
        public long ClosedAlertId { get; set; }
        public long OriginalAlertId { get; set; }
        public long ProductId { get; set; }
        public long ShelfId { get; set; }

        public DateTime PredictedDepletionDate { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FulfillmentNote { get; set; }

        public DateTime CreatedAt { get; set; } // When the original alert was created
        public DateTime ClosedAt { get; set; }  // When the request was marked delivered
    }
}