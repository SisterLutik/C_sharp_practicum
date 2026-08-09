using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventService _eventService;
        private static readonly object _bookingLock = new();  // ← static!

        public BookingService(IBookingRepository bookingRepository, IEventService eventService)
        {
            _bookingRepository = bookingRepository;
            _eventService = eventService;
        }

        public Booking CreateBooking(Guid eventId)
        {
            lock (_bookingLock)  // ← общий для всех запросов
            {
                var eventExists = _eventService.GetById(eventId);
                if (eventExists == null)
                    throw new BusinessException($"Событие с id {eventId} не найдено", 404);

                if (!eventExists.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

                _eventService.Update(eventId, eventExists);

                var booking = new Booking (eventId);
                _bookingRepository.Add(booking);
                return booking;
            }
        }

        public Booking? GetBookingById(Guid bookingId)
        {
            var booking = _bookingRepository.GetById(bookingId);
            if (booking == null)
                throw new BusinessException($"Бронь с id {bookingId} не найдена", 404);
            return booking;
        }
    }
}