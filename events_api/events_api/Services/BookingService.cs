using events_api.events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventService _eventService;

        public BookingService(
            IBookingRepository bookingRepository,
            IEventService eventService)
        {
            _bookingRepository = bookingRepository;
            _eventService = eventService;
        }

        public async Task<Booking> CreateBookingAsync(int eventId)
        {
            var eventExists = _eventService.GetById(eventId);
            if (eventExists == null)
                throw new BusinessException($"Событие с id {eventId} не найдено", 404);

            var booking = new Booking
            {
                EventId = eventId
            };

            _bookingRepository.Add(booking);
            return booking;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = _bookingRepository.GetById(bookingId);

            if (booking == null)
                throw new BusinessException($"Бронь с id {bookingId} не найдена", 404);

            return booking;
        }
    }
}