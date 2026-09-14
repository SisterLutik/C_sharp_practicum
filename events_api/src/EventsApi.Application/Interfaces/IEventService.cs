using EventsApi.Domain.Entities;
using EventsApi.Application.DTOs;

namespace EventsApi.Application.Interfaces
{
    public interface IEventService
    {
        Task<Event?> GetByIdAsync(Guid id);
        Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
        Task<Event> UpdateAsync(Guid id, Event updatedEvent);
        Task DeleteAsync(Guid id);
    }
}