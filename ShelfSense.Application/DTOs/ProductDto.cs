using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public static class ProductDto
    {
        public class ProductCreateRequest
        {
            [Required]
            [StringLength(50)]
            public string StockKeepingUnit { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string ProductName { get; set; } = string.Empty;

            [Required]
            public long CategoryId { get; set; }

            [StringLength(50)]
            public string? PackageSize { get; set; }

            [StringLength(20)]
            public string? Unit { get; set; }
        }

        public class ProductResponse
        {
            public long ProductId { get; set; }
            public string StockKeepingUnit { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public long CategoryId { get; set; }
            public string? PackageSize { get; set; }
            public string? Unit { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
