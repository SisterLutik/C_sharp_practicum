using EventService.Domain.Entities;
using EventsApi.Contracts.Events;
using EventService.Application.DTOs;

namespace EventService.Application.Interfaces;

public interface IEventService
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task<Event> UpdateAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task DeleteAsync(Guid id);
    Task<ReserveSeatsResponse> TryReserveSeatsAsync(Guid eventId);
    Task<bool> ReleaseSeatsAsync(Guid eventId);
}