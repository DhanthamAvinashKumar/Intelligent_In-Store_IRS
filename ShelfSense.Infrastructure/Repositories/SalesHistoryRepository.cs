using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Infrastructure.Repositories
{
    public class SalesHistoryRepository : ISalesHistory
    {
        private readonly ShelfSenseDbContext _context;

        public SalesHistoryRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public IQueryable<SalesHistory> GetAll() => _context.SalesHistories.AsQueryable();

        public async Task<SalesHistory?> GetByIdAsync(long id) =>
            await _context.SalesHistories.FindAsync(id);

        public async Task AddAsync(SalesHistory entity)
        {
            await _context.SalesHistories.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SalesHistory entity)
        {
            _context.SalesHistories.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.SalesHistories.FindAsync(id);
            if (entity != null)
            {
                _context.SalesHistories.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

}
