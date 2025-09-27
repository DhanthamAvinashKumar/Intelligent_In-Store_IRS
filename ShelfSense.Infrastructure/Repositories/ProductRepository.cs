using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShelfSenseDbContext _context;

        public ProductRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public IQueryable<Product> GetAll() => _context.Products.AsQueryable();

        public async Task<Product?> GetByIdAsync(long id) =>
            await _context.Products.FindAsync(id);

        public async Task<Product?> GetBySkuAsync(string sku) =>
            await _context.Products.FirstOrDefaultAsync(p => p.StockKeepingUnit == sku);

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
