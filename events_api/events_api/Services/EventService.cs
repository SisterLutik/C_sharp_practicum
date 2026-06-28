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
                Id = 1,
                Title = "Интенсив по ловле жуков",
                Description = "Увлекательный аттракцион",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now
            });
            _events.Add(new Event
            {
                Id = 2,
                Title = "Мозгорадная вечеринка",
                Description = "Думаем сразу много мыслей",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(10)
            });
        }

        public Event? GetById(int id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public List<Event> GetAll(string? title, DateTime? from, DateTime? to)
        {
            var query = _events.AsQueryable();

            // Фильтр по названию (регистронезависимый, частичное совпадение)
            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }

            // Фильтр по дате начала (не раньше указанной даты)
            if (from.HasValue)
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }

            // Фильтр по дате окончания (не позже указанной даты)
            if (to.HasValue)
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }

            return query.ToList();
        }

        public void Add(Event eventItem)
        {
            eventItem.Id = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;
            _events.Add(eventItem);
        }

        public void Update(int id, Event updatedEvent)
        {
            var eventItem = GetById(id);
            if (eventItem == null)
            {
                Console.WriteLine($"Событие #{id} не найдено");
                return;
            }

            // Все поля обязательны
            eventItem.Title = updatedEvent.Title;
            eventItem.Description = updatedEvent.Description;
            eventItem.StartAt = updatedEvent.StartAt;
            eventItem.EndAt = updatedEvent.EndAt;

            if (eventItem.EndAt < eventItem.StartAt)
                throw new InvalidOperationException("EndAt не может быть меньше StartAt");

            Console.WriteLine($"Событие #{eventItem.Id} успешно обновлено");
        }


        public void Delete(int id)
        {
            var eventItem = GetById(id);
            if (eventItem != null)
                _events.Remove(eventItem);
        }
    }
}