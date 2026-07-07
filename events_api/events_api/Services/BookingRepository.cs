using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingRepository : IBookingRepository
    {
        private readonly List<Booking> _bookings = new();

        public BookingRepository()
        {
            // Имитация начальных данных
            _bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                EventId = 1,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ProcessedAt = DateTime.UtcNow.AddDays(-4)
            });
            _bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                EventId = 1,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }

        public Booking? GetById(Guid id)
        {
            Console.WriteLine($"[BookingRepository] Получение брони #{id}");
            return _bookings.FirstOrDefault(b => b.Id == id);
        }

        public List<Booking> GetAll()
        {
            Console.WriteLine("[BookingRepository] Получение всех броней");
            return _bookings;
        }

        public void Add(Booking booking)
        {
            // ✅ Уникальный Id (Guid.NewGuid())
            booking.Id = Guid.NewGuid();

            // ✅ Текущая дата в CreatedAt
            booking.CreatedAt = DateTime.UtcNow;

            // ✅ Статус Pending при создании
            booking.Status = BookingStatus.Pending;

            _bookings.Add(booking);
            Console.WriteLine($"[BookingRepository] Бронь #{booking.Id} добавлена со статусом Pending");
        }

        public void Update(Booking booking)
        {
            var existingBooking = GetById(booking.Id);
            if (existingBooking == null)
            {
                Console.WriteLine($"[BookingRepository] Бронь #{booking.Id} не найдена");
                return;
            }

            // Обновляем статус
            existingBooking.Status = booking.Status;

            // Если статус изменился на Confirmed или Rejected — заполняем ProcessedAt
            if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Rejected)
            {
                existingBooking.ProcessedAt = DateTime.UtcNow;
            }

            Console.WriteLine($"[BookingRepository] Бронь #{booking.Id} обновлена. Новый статус: {booking.Status}");
        }

        public void Delete(Guid id)
        {
            var booking = GetById(id);
            if (booking != null)
            {
                _bookings.Remove(booking);
                Console.WriteLine($"[BookingRepository] Бронь #{id} удалена");
            }
            else
            {
                Console.WriteLine($"[BookingRepository] Бронь #{id} не найдена для удаления");
            }
        }

        public List<Booking> GetByEventId(int eventId)
        {
            Console.WriteLine($"[BookingRepository] Получение броней для события #{eventId}");
            return _bookings.Where(b => b.EventId == eventId).ToList();
        }
    }
}