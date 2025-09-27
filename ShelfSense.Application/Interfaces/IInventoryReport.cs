using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IInventoryReport
    {
        IQueryable<InventoryReport> GetAll();
        Task<InventoryReport?> GetByIdAsync(long id);
        Task AddAsync(InventoryReport entity);
        Task UpdateAsync(InventoryReport entity);
        Task DeleteAsync(long id);
    }

}
