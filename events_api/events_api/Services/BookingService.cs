using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventService _eventService;
        private readonly object _bookingLock = new();

        public BookingService(IBookingRepository bookingRepository, IEventService eventService)
        {
            _bookingRepository = bookingRepository;
            _eventService = eventService;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            lock (_bookingLock)
            {
                // 1. Получаем событие
                var eventExists = _eventService.GetById(eventId);
                if (eventExists == null)
                    throw new BusinessException($"Событие с id {eventId} не найдено", 404);

                // 2. Пытаемся зарезервировать место
                if (!eventExists.TryReserveSeats())
                    throw new NoAvailableSeatsException($"Нет свободных мест для события {eventId}");

                // 3. Сохраняем обновлённое событие
                _eventService.Update(eventId, eventExists);

                // 4. Создаём бронь
                var booking = new Booking
                {
                    EventId = eventId
                };

                _bookingRepository.Add(booking);
                return booking;
            }
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