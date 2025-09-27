using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class ProductShelfRepository : IProductShelfRepository
{
    private readonly ShelfSenseDbContext _context;

    public ProductShelfRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<ProductShelf> GetAll() => _context.ProductShelves.AsQueryable();

    public async Task<ProductShelf?> GetByIdAsync(long id) =>
        await _context.ProductShelves.FindAsync(id);

    public async Task AddAsync(ProductShelf entity)
    {
        await _context.ProductShelves.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductShelf entity)
    {
        _context.ProductShelves.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.ProductShelves.FindAsync(id);
        if (entity != null)
        {
            _context.ProductShelves.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
