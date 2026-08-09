using events_api.Interfaces;
using events_api.Models;

namespace events_api.Data
{
     public class BookingRepository : IBookingRepository
    {
        private readonly List<Booking> _bookings = new();
        private readonly ILogger<BookingRepository> _logger;


        public BookingRepository(ILogger<BookingRepository> logger)
        {
            _logger = logger;
            // Имитация начальных данных
            _bookings.Add(new Booking(
                Guid.NewGuid()
                ));
            _bookings.Add(new Booking(
                Guid.NewGuid()
                ));
        }

        public Booking? GetById(Guid id)
        {
            _logger.LogInformation($"[BookingRepository] Получение брони #{id}");
            return _bookings.FirstOrDefault(b => b.Id == id);
        }

        public List<Booking> GetAll()
        {
            _logger.LogInformation($"[BookingRepository] Получение всех броней");
            return _bookings;
        }

        public void Add(Booking booking)
        {
            booking.Id = Guid.NewGuid();
            booking.CreatedAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Pending;

            _bookings.Add(booking);
            _logger.LogInformation($"Бронь #{booking.Id} добавлена со статусом Pending");
        }

        public void Update(Booking booking)
        {
            var existingBooking = GetById(booking.Id);
            if (existingBooking == null)
            {
                _logger.LogInformation($"[BookingRepository] Бронь #{booking.Id} не найдена");
                return;
            }

            existingBooking.Status = booking.Status;

            if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Rejected)
            {
                existingBooking.ProcessedAt = DateTime.UtcNow;
            }
            _logger.LogInformation($"[BookingRepository] Бронь #{booking.Id} обновлена. Новый статус: {booking.Status}");
        }

        public void Delete(Guid id)
        {
            var booking = GetById(id);
            if (booking != null)
            {
                _bookings.Remove(booking);
                _logger.LogInformation($"[BookingRepository] Бронь #{id} удалена");
            }
            else
            {
                _logger.LogInformation($"[BookingRepository] Бронь #{id} не найдена для удаления");
            }
        }

        public List<Booking> GetByEventId(Guid eventId)
        {
            _logger.LogInformation($"[BookingRepository] Получение броней для события #{eventId}");
            return _bookings.Where(b => b.EventId == eventId).ToList();
        }

        public List<Booking> GetByStatus(BookingStatus status)
        {
            _logger.LogInformation($"[BookingRepository] Получение броней со статусом {status}");
            return _bookings.Where(b => b.Status == status).ToList();
        }
    }
}