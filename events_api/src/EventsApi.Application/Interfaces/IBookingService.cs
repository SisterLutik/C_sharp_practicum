using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;

namespace EventsApi.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, UserRole role);
}