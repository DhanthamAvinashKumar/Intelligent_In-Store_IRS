using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public static class CategoryDto
    {
        public class CategoryCreateRequest
        {
            [Required]
            [StringLength(100)]
            public string CategoryName { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }
        }

        public class CategoryResponse
        {
            public long CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
