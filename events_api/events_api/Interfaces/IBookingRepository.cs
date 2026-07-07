using events_api.Models;

namespace events_api.Interfaces
{
    public interface IBookingRepository
    {
        Booking? GetById(Guid id);
        List<Booking> GetAll();
        void Add(Booking booking);
        void Update(Booking booking);
        void Delete(Guid id);
        List<Booking> GetByEventId(Guid eventId);
        List<Booking> GetByStatus(BookingStatus status);
    }
}