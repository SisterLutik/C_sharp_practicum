using events_api.events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = new();

        public EventService()
        {
            _events.Add(new Event
            {
                Id = Guid.NewGuid(),        
                Title = "Интенсив по ловле жуков",
                Description = "Увлекательный аттракцион",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now
            });
            _events.Add(new Event
            {
                Id = Guid.NewGuid(), 
                Title = "Мозговая вечеринка",
                Description = "Думаем сразу много мыслей",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(10)
            });
        }

        public Event? GetById(Guid id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var query = _events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                query = query.Where(e => e.StartAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.EndAt <= to.Value);

            var totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<Event>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        public void Add(Event eventItem)
        {
            eventItem.Id = Guid.NewGuid();  
            _events.Add(eventItem);
        }

        public void Update(Guid id, Event updatedEvent)
        {
            var existingEvent = GetById(id);

            if (existingEvent == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;

            // Проверка дат
            if (existingEvent.EndAt <= existingEvent.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);
        }

        public void Delete(Guid id)
        {
            var eventItem = GetById(id);
            if (eventItem == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);
            _events.Remove(eventItem);
        }
    }
}