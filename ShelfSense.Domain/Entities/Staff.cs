using System.ComponentModel.DataAnnotations;

public class Staff
{
    public long StaffId { get; set; }

    [Required]
    public long StoreId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("staff|manager")]
    public string Role { get; set; } = "staff";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Store? Store { get; set; }
}
