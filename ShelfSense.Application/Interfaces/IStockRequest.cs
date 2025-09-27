using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IStockRequest
    {
        IQueryable<StockRequest> GetAll();
        Task<StockRequest?> GetByIdAsync(long id);
        Task AddAsync(StockRequest entity);
        Task UpdateAsync(StockRequest entity);
        Task DeleteAsync(long id);
    }


}
