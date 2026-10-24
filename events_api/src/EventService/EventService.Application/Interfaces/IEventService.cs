using EventService.Application.DTOs;
using EventService.Domain.Entities;

namespace EventService.Application.Interfaces;

public interface IEventService
{
    Task<EventResponse?> GetByIdAsync(Guid id);

    Task<List<EventResponse>> GetTopEventsAsync(int count = 10);

    Task<PaginatedResult<Event>> GetAllAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize);

    Task<Event> CreateEventAsync(
        string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);

    Task<Event> UpdateAsync(
        Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);

    Task DeleteAsync(Guid id);
}