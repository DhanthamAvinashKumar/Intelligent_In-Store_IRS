using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class Shelf
{
    public long ShelfId { get; set; }

    [Required]
    [StringLength(50)]
    public string ShelfCode { get; set; } = string.Empty;

    [Required]
    public long StoreId { get; set; }

    [Required] // ✅ Add this
    public long CategoryId { get; set; }

    [StringLength(100)]
    public string? LocationDescription { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } // ✅ New field

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Store? Store { get; set; }
    public Category? Category { get; set; } // ✅ Add this
}