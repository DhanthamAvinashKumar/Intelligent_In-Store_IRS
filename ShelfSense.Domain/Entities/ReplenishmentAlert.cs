using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class ReplenishmentAlert
{
    public long AlertId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public long ShelfId { get; set; }

    [Required]
    public DateTime PredictedDepletionDate { get; set; }

    [Required]
    [EnumDataType(typeof(UrgencyLevel))]
    public string UrgencyLevel { get; set; } = "low";

    [Required]
    [EnumDataType(typeof(AlertStatus))]
    public string Status { get; set; } = "open";

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public Shelf? Shelf { get; set; }
}

public enum UrgencyLevel
{
    low,
    medium,
    high,
    critical
}

public enum AlertStatus
{
    open,
    acknowledged,
    resolved
}
