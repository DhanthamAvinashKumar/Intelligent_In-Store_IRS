using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class SalesHistory
{
    public long SaleId { get; set; }

    [Required]
    public long StoreId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateTime SaleTime { get; set; }

    // Navigation
    public Store? Store { get; set; }
    public Product? Product { get; set; }
}
