 
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

        [StringLength(100)]
        public string? LocationDescription { get; set; }
    }

    public class ShelfResponse
    {
        public long ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public long StoreId { get; set; }
        public string? LocationDescription { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
