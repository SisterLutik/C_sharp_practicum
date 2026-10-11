using EventService.Domain.Entities;
using EventService.Application.DTOs;

namespace EventService.Application.Interfaces
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