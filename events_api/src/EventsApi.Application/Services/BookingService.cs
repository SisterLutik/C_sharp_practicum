using EventsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using EventsApi.Domain.Exceptions;
using EventsApi.Domain.Entities;

namespace EventsApi.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingService> _logger;
        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public BookingService(
            IEventRepository eventRepository,
            IBookingRepository bookingRepository,
            ILogger<BookingService> logger)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            await _bookingSemaphore.WaitAsync();

            try
            {
                var eventExists = await _eventRepository.GetByIdAsync(eventId);
                if (eventExists == null)
                    throw new BusinessException($"Событие с id {eventId} не найдено", 404);

                if (!eventExists.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

                await _eventRepository.UpdateAsync(eventExists);

                var booking = new Booking(eventId);
                await _bookingRepository.AddAsync(booking);

                _logger.LogInformation($"Создана бронь {booking.Id} для события {eventId}");
                return booking;
            }
            finally
            {
                _bookingSemaphore.Release();
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
                throw new BusinessException($"Бронь с id {bookingId} не найдена", 404);

            return booking;
        }
    }
}