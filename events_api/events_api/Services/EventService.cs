using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace events_api.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EventService> _logger;

        public EventService(AppDbContext context, ILogger<EventService> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<Event> CreateEventAsync(
            string title,
            string? description,
            DateTime startAt,
            DateTime endAt,
            int totalSeats)
        {
            if (totalSeats <= 0)
                throw new BusinessException("TotalSeats должен быть больше 0", 400);

            if (endAt <= startAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            var newEvent = new Event(title, startAt, endAt, totalSeats, description);
            await _context.Events.AddAsync(newEvent);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Создано событие {newEvent.Id}");

            return newEvent;
        }

        public async Task<Event> UpdateAsync(Guid id, Event updatedEvent)
        {
            var existingEvent = await GetByIdAsync(id);
            if (existingEvent == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            existingEvent.UpdateDetails(
                updatedEvent.Title,
                updatedEvent.Description,
                updatedEvent.StartAt,
                updatedEvent.EndAt,
                updatedEvent.TotalSeats
            );

            if (existingEvent.EndAt <= existingEvent.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Обновлено событие {id}");

            return existingEvent;
        }

        public async Task DeleteAsync(Guid id)
        {
            var eventItem = await GetByIdAsync(id);
            if (eventItem == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Удалено событие {id}");
        }
    }
}