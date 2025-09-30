using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class StockDepletionPredictionDto
    {
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public int Quantity { get; set; }
        public double SalesVelocity { get; set; }
        public double DaysToDepletion { get; set; }
        public DateTime? ExpectedDepletionDate { get; set; }
        public bool IsLowStock { get; set; }
    }
}
