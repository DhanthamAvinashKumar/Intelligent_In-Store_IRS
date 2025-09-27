using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface ISalesHistory
    {
        IQueryable<SalesHistory> GetAll();
        Task<SalesHistory?> GetByIdAsync(long id);
        Task AddAsync(SalesHistory entity);
        Task UpdateAsync(SalesHistory entity);
        Task DeleteAsync(long id);
    }

}
