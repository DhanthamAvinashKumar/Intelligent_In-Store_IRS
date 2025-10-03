using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class DeliveredStockRequestDto
    {
        public long DeliveredRequestId { get; set; }
        public long OriginalRequestId { get; set; }
        public long ProductId { get; set; }
        public long StoreId { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveredAt { get; set; }
        public long? AlertId { get; set; }
        public string? Notes { get; set; }
    }

}
