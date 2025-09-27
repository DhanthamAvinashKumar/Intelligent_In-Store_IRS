using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Infrastructure.Repositories
{
    public class RestockTaskRepository : IRestockTaskRepository
    {
        private readonly ShelfSenseDbContext _context;

        public RestockTaskRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public IQueryable<RestockTask> GetAll() => _context.RestockTasks.AsQueryable();

        public async Task<RestockTask?> GetByIdAsync(long id) =>
            await _context.RestockTasks.FindAsync(id);

        public async Task AddAsync(RestockTask entity)
        {
            await _context.RestockTasks.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RestockTask entity)
        {
            _context.RestockTasks.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.RestockTasks.FindAsync(id);
            if (entity != null)
            {
                _context.RestockTasks.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

}
