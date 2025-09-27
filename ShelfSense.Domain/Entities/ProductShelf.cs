using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class ProductShelf
{
    public long ProductShelfId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public long ShelfId { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime LastRestockedAt { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public Shelf? Shelf { get; set; }
}