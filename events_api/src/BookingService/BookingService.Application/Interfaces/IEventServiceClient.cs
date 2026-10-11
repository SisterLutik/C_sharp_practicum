using EventsApi.Contracts.Events;

namespace BookingService.Application.Interfaces;

public interface IEventServiceClient
{
    Task<EventDto?> GetEventAsync(Guid eventId);
    Task<ReserveSeatsResponse> TryReserveSeatsAsync(Guid eventId);
    Task ReleaseSeatsAsync(Guid eventId);
}