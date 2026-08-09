using events_api.Exceptions; 
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = new();

        public EventService()
        {
            _events.Add(new Event(

            "Интенсив по ловле жуков",
            DateTime.Now.AddDays(-2),
            DateTime.Now,
            50,
            "Увлекательный аттракцион"
                ));
            _events.Add(new Event(
               "Мозговая вечеринка",
                DateTime.Now.AddDays(3),
                DateTime.Now.AddDays(10),
                30,
                "Думаем сразу много мыслей"

            ));
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

        public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            if (totalSeats <= 0)
                throw new BusinessException("TotalSeats должен быть больше 0", 400);

            if (endAt <= startAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            var newEvent = new Event(
                title,
                startAt,
                endAt,
                totalSeats,
                description
            );

            Add(newEvent);
            return newEvent;
        }

        public void Add(Event eventItem)
        {
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
            existingEvent.TotalSeats = updatedEvent.TotalSeats;
            existingEvent.AvailableSeats = updatedEvent.AvailableSeats;

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