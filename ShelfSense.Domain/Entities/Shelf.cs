using System.ComponentModel.DataAnnotations;

public class Shelf
{
    public long ShelfId { get; set; }

    [Required]
    [StringLength(50)]
    public string ShelfCode { get; set; } = string.Empty;

    [Required]
    public long StoreId { get; set; }

    [StringLength(100)]
    public string? LocationDescription { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Store? Store { get; set; }
}
