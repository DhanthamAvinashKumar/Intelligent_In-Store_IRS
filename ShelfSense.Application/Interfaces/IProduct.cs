using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IProductRepository
    {
        IQueryable<Product> GetAll();
        Task<Product?> GetByIdAsync(long id);
        Task<Product?> GetBySkuAsync(string sku);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(long id);
    }
}
