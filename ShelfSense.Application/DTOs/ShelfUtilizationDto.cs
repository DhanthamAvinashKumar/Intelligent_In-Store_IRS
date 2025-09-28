using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class ShelfUtilizationDto
    {
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public int Quantity { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent { get; set; }
        public int SalesCountLast7Days { get; set; }
        public DateTime? LastSaleTime { get; set; }
    }

}

