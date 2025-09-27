using System.ComponentModel.DataAnnotations;

public class InventoryReportCreateRequest
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    public long ShelfId { get; set; }

    [Required]
    public DateTime ReportDate { get; set; }

    [Required]
    public int QuantityOnShelf { get; set; }

    public int? QuantityRestocked { get; set; }

    public bool AlertTriggered { get; set; }
}

public class InventoryReportResponse
{
    public long ReportId { get; set; }
    public long ProductId { get; set; }
    public long ShelfId { get; set; }
    public DateTime ReportDate { get; set; }
    public int QuantityOnShelf { get; set; }
    public int? QuantityRestocked { get; set; }
    public bool AlertTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
}
