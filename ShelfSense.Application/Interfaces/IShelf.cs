using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IShelfRepository
    {
        IQueryable<Shelf> GetAll();
        Task<Shelf?> GetByIdAsync(long id);
        Task AddAsync(Shelf shelf);
        Task UpdateAsync(Shelf shelf);
        Task DeleteAsync(long id);
    }
}
