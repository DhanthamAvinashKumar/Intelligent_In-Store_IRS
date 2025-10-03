using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DeliveredStockRequest
{
    [Key]
    public long DeliveredRequestId { get; set; }

    [Required]
    public long OriginalRequestId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public long StoreId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateTime DeliveredAt { get; set; }

    public long? AlertId { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    // Optional navigation
    public Product? Product { get; set; }
    public Store? Store { get; set; }
}
