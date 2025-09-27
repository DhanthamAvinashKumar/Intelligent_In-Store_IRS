using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IProductShelfRepository
    {
        IQueryable<ProductShelf> GetAll();
        Task<ProductShelf?> GetByIdAsync(long id);
        Task AddAsync(ProductShelf entity);
        Task UpdateAsync(ProductShelf entity);
        Task DeleteAsync(long id);
    }
}
