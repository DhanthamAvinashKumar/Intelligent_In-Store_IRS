using System.ComponentModel.DataAnnotations;

public class Store
{
    public long StoreId { get; set; }

    [Required]
    [StringLength(100)]
    public string StoreName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    public DateTime CreatedAt { get; set; }
}
