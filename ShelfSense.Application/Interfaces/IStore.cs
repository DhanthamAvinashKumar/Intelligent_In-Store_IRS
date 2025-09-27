using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IStoreRepository
    {
        IQueryable<Store> GetAll();
        Task<Store?> GetByIdAsync(long id);
        Task AddAsync(Store store);
        Task UpdateAsync(Store store);
        Task DeleteAsync(long id);
    }

}
