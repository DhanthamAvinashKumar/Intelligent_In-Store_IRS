using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IReplenishmentAlert
    {
        IQueryable<ReplenishmentAlert> GetAll();
        Task<ReplenishmentAlert?> GetByIdAsync(long id);
        Task AddAsync(ReplenishmentAlert entity);
        Task UpdateAsync(ReplenishmentAlert entity);
        Task DeleteAsync(long id);
    }
}
