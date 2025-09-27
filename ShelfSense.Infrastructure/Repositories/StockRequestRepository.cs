using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class StockRequestRepository : IStockRequest
{
    private readonly ShelfSenseDbContext _context;

    public StockRequestRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<StockRequest> GetAll() => _context.StockRequests.AsQueryable();

    public async Task<StockRequest?> GetByIdAsync(long id) =>
        await _context.StockRequests.FindAsync(id);

    public async Task AddAsync(StockRequest entity)
    {
        await _context.StockRequests.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(StockRequest entity)
    {
        _context.StockRequests.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.StockRequests.FindAsync(id);
        if (entity != null)
        {
            _context.StockRequests.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
