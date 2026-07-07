using events_api.Models;


namespace events_api.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(int eventId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    }
}