using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

public class StaffRepository : IStaffRepository
{
    private readonly ShelfSenseDbContext _context;

    public StaffRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    public IQueryable<Staff> GetAll() => _context.Staffs.AsQueryable();

    public async Task<Staff?> GetByIdAsync(long id) =>
        await _context.Staffs.FindAsync(id);

    public async Task AddAsync(Staff entity)
    {
        await _context.Staffs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Staff entity)
    {
        _context.Staffs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.Staffs.FindAsync(id);
        if (entity != null)
        {
            _context.Staffs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
