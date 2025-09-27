using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
 

public class ReplenishmentAlertRepository : IReplenishmentAlert
{
    private readonly ShelfSenseDbContext _context;

    public ReplenishmentAlertRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<ReplenishmentAlert> GetAll() => _context.ReplenishmentAlerts.AsQueryable();

    public async Task<ReplenishmentAlert?> GetByIdAsync(long id) =>
        await _context.ReplenishmentAlerts.FindAsync(id);

    public async Task AddAsync(ReplenishmentAlert entity)
    {
        await _context.ReplenishmentAlerts.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ReplenishmentAlert entity)
    {
        _context.ReplenishmentAlerts.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.ReplenishmentAlerts.FindAsync(id);
        if (entity != null)
        {
            _context.ReplenishmentAlerts.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
