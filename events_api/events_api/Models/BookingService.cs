using events_api.events_api.Exceptions;
using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }

        public async Task<Booking> CreateBookingAsync(int eventId)
        {
            // Проверяем, существует ли событие
            var eventExists = _eventRepository.GetById(eventId);
            if (eventExists == null)
                throw new BusinessException($"Событие с id {eventId} не найдено", 404);

            // Создаём бронь (Id, CreatedAt, Status Pending — генерируются в репозитории)
            var booking = new Booking
            {
                EventId = eventId
            };

            // Сохраняем бронь (репозиторий сам установит Id, CreatedAt и Status)
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