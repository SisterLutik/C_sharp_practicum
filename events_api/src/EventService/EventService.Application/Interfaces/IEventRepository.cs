using EventService.Application.DTOs;
using EventService.Domain.Entities;

namespace EventService.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<List<Event>> GetTopBySalesPercentageAsync(int count, CancellationToken cancellationToken = default);   // ← НОВЫЙ
    Task AddAsync(Event eventItem);
    Task UpdateAsync(Event eventItem);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}