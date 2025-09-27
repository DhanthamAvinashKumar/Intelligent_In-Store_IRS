using System.ComponentModel.DataAnnotations;

public class StaffCreateRequest
{
    [Required]
    public long StoreId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [RegularExpression("staff|manager")]
    public string Role { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }
}

public class StaffResponse
{
    public long StaffId { get; set; }
    public long StoreId { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
