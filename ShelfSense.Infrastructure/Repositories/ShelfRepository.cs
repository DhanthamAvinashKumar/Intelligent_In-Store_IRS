using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class ShelfRepository : IShelfRepository
{
    private readonly ShelfSenseDbContext _context;

    public ShelfRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<Shelf> GetAll() => _context.Shelves.AsQueryable();

    public async Task<Shelf?> GetByIdAsync(long id) =>
        await _context.Shelves.FindAsync(id);

    public async Task AddAsync(Shelf shelf)
    {
        await _context.Shelves.AddAsync(shelf);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shelf shelf)
    {
        _context.Shelves.Update(shelf);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var shelf = await _context.Shelves.FindAsync(id);
        if (shelf != null)
        {
            _context.Shelves.Remove(shelf);
            await _context.SaveChangesAsync();
        }
    }
}
