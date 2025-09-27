using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ShelfSenseDbContext _context;

        public CategoryRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public IQueryable<Category> GetAll() => _context.Categories.AsQueryable();

        public async Task<Category?> GetByIdAsync(long id) =>
            await _context.Categories.FindAsync(id);

        public async Task<Category?> GetByNameAsync(string name) =>
            await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == name);

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
