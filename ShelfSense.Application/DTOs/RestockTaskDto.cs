using System.ComponentModel.DataAnnotations;

public class RestockTaskCreateRequest
{
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

    public DateTime? CompletedAt { get; set; }
}

public class RestockTaskResponse
{
    public long TaskId { get; set; }
    public long AlertId { get; set; }
    public long ProductId { get; set; }
    public long ShelfId { get; set; }
    public long AssignedTo { get; set; }
    public string Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
