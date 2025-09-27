using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class StoreRepository : IStoreRepository
{
    private readonly ShelfSenseDbContext _context;

    public StoreRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<Store> GetAll() => _context.Stores.AsQueryable();

    public async Task<Store?> GetByIdAsync(long id) =>
        await _context.Stores.FindAsync(id);

    public async Task AddAsync(Store store)
    {
        await _context.Stores.AddAsync(store);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Store store)
    {
        _context.Stores.Update(store);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store != null)
        {
            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
        }
    }
}
