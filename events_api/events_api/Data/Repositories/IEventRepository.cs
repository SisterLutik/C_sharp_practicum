using events_api.Models;

namespace events_api.Data.Repositories
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(Guid id);
        Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        Task AddAsync(Event eventItem);
        Task UpdateAsync(Event eventItem);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}