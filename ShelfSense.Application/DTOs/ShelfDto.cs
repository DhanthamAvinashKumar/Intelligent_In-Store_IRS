 
using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public class ShelfCreateRequest
    {
        [Required]
        [StringLength(50)]
        public string ShelfCode { get; set; } = string.Empty;

        [Required]
        public long StoreId { get; set; }

        [Required]
        public long CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; } // ✅ New field

        [StringLength(100)]
        public string? LocationDescription { get; set; }
    }

    public class ShelfResponse
    {
        public long ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public long StoreId { get; set; }
        public long CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; } // ✅ New field
        public string? LocationDescription { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
