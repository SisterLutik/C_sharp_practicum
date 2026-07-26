using events_api.Models;


namespace events_api.Interfaces
{
    public interface IBookingService
    {
        Booking CreateBooking(Guid eventId);
        Booking? GetBookingById(Guid bookingId);
    }
}