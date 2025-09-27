using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class StockRequest
{
    public long RequestId { get; set; }

    [Required]
    public long StoreId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    public DateTime RequestDate { get; set; }

    [Required]
    [RegularExpression("requested|in_transit|delivered|cancelled")]
    public string DeliveryStatus { get; set; } = "requested";

    public DateTime? EstimatedTimeOfArrival { get; set; }

    // Navigation
    public Store? Store { get; set; }
    public Product? Product { get; set; }
}
