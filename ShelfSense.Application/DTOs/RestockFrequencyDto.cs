using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class RestockFrequencyDto
    {
        public int ProductId { get; set; }
        public int ShelfId { get; set; }
        public int AlertCount { get; set; }
        public int TotalDays { get; set; }
        public double AvgRestockFrequencyDays { get; set; }
    }

}
