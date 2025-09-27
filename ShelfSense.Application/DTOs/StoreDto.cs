using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public class StoreCreateRequest
    {
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
    }

    public class StoreResponse
    {
        public long StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
