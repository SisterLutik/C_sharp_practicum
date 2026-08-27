using events_api.Models;
using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;


namespace events_api.Data.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;  

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(Guid id)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<Booking>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.Bookings
                .Where(b => b.EventId == eventId)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetPendingAsync()
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Status == BookingStatus.Pending)
                .ToListAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var booking = await GetByIdAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Bookings.AnyAsync(b => b.Id == id);
        }
    }
}