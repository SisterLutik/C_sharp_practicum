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

        public List<Event> GetAll()
        {
            return _events;
        }

        public void Add(Event eventItem)
        {
            eventItem.Id = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;
            _events.Add(eventItem);
        }

        public void Update(int id, Event updatedEvent)
        {
            var existingEvent = GetById(id);
            if (existingEvent == null) return;

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;
        }

        public void Delete(int id)
        {
            var eventItem = GetById(id);
            if (eventItem != null)
                _events.Remove(eventItem);
        }
    }
}