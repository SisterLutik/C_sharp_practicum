using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace events_api.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public BookingService(AppDbContext context, ILogger<BookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            await _bookingSemaphore.WaitAsync();

            try
            {
                var eventExists = await _context.Events
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (eventExists == null)
                    throw new BusinessException($"Событие с id {eventId} не найдено", 404);

                if (!eventExists.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

                var booking = new Booking(eventId);
                await _context.Bookings.AddAsync(booking);

                // ✅ Один вызов сохраняет и бронь, и изменение AvailableSeats
                await _context.SaveChangesAsync();

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
            return await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }
    }
}