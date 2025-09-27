using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public class ProductShelfCreateRequest
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public long ShelfId { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class ProductShelfResponse
    {
        public long ProductShelfId { get; set; }
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastRestockedAt { get; set; }
    }
}
