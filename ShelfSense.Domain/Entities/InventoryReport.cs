using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class InventoryReport
{
    public long ReportId { get; set; }

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

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public Shelf? Shelf { get; set; }
}
