using System.ComponentModel.DataAnnotations;

public class StockRequestCreateRequest
{
    [Required]
    public long StoreId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [RegularExpression("requested|in_transit|delivered|cancelled")]
    public string DeliveryStatus { get; set; } = "requested";

    public DateTime? EstimatedTimeOfArrival { get; set; }
}

public class StockRequestResponse
{
    public long RequestId { get; set; }
    public long StoreId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? EstimatedTimeOfArrival { get; set; }
}
