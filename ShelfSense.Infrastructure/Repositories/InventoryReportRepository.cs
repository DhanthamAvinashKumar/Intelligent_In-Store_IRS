using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class InventoryReportRepository : IInventoryReport
{
    private readonly ShelfSenseDbContext _context;

    public InventoryReportRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<InventoryReport> GetAll() => _context.InventoryReports.AsQueryable();

    public async Task<InventoryReport?> GetByIdAsync(long id) =>
        await _context.InventoryReports.FindAsync(id);

    public async Task AddAsync(InventoryReport entity)
    {
        await _context.InventoryReports.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InventoryReport entity)
    {
        _context.InventoryReports.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.InventoryReports.FindAsync(id);
        if (entity != null)
        {
            _context.InventoryReports.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
