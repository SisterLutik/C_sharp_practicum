using events_api.Models;
using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace events_api.Data.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Event?> GetByIdAsync(Guid id)
        {
            return await _context.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<PaginatedResult<Event>> GetAllAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                query = query.Where(e => e.StartAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.EndAt <= to.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<Event>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        public async Task AddAsync(Event eventItem)
        {
            await _context.Events.AddAsync(eventItem);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Event eventItem)
        {
            _context.Events.Update(eventItem);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var eventItem = await GetByIdAsync(id);
            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Events.AnyAsync(e => e.Id == id);
        }
    }
}