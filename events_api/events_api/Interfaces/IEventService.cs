using events_api.Models;

namespace events_api.Interfaces
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