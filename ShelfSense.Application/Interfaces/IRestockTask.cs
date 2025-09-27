using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IRestockTaskRepository
    {
        IQueryable<RestockTask> GetAll();
        Task<RestockTask?> GetByIdAsync(long id);
        Task AddAsync(RestockTask entity);
        Task UpdateAsync(RestockTask entity);
        Task DeleteAsync(long id);
    }
}
