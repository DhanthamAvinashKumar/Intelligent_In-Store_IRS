using ShelfSense.Domain.Entities;
using System.Linq;

namespace ShelfSense.Application.Interfaces
{
    public interface ICategoryRepository
    {
        IQueryable<Category> GetAll();
        Task<Category?> GetByIdAsync(long id);
        Task<Category?> GetByNameAsync(string name);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(long id);
    }
}
