using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
    Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, bool isAdmin);
}