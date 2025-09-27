using ShelfSense.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class RestockTask
{
    public long TaskId { get; set; }

    [Required]
    public long AlertId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public long ShelfId { get; set; }

    [Required]
    public long AssignedTo { get; set; }

    [Required]
    [RegularExpression("pending|in_progress|completed|delayed")]
    public string Status { get; set; } = "pending";

    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ReplenishmentAlert? Alert { get; set; }
    public Product? Product { get; set; }
    public Shelf? Shelf { get; set; }
    public Staff? Staff { get; set; }
}
