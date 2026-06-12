using events_api.Interfaces;
using events_api.Models;

namespace events_api.Data
{
    public class EventRepository : IEventService
    {
        private readonly List<Event> _events = new();
        public EventRepository()
        {
            // Имитация данных из БД
            _events.Add(new Event
            {
                Id = 1,
                Title = "Интенсив по ловли жуков",
                Description = "Увлекательный аттракцион",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now
            });
            _events.Add(new Event
            {
                Id = 1,
                Title = "Мозгарадная вечеринка",
                Description = "Думаем сразу много мыслей",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(10)
            });
        }
    

     public Event? GetById(int id)
        {
            Console.WriteLine($"[EventRepository] Получение события #{id} из базы данных");
            return _events.FirstOrDefault(o => o.Id == id);
        }

        public List<Event> GetAll()
        {
            Console.WriteLine("[EventRepository] Получение всех событий из базы данных");
            return _events;
        }

        public void Add(Event eventItem)
        {
            eventItem.Id = _events.Any() ? _events.Max(o => o.Id) + 1 : 1;
            _events.Add(eventItem);
            Console.WriteLine($"[EventRepository] Событие #{eventItem.Id} добавлен в базу данных");
        }

        public void Update(Event eventItem)
        {
            var existing = _events.FirstOrDefault(o => o.Id == eventItem.Id);
            if (existing != null)
            {
                existing.Title = eventItem.Title;
                existing.Description = eventItem.Description;
                existing.StartAt = eventItem.StartAt;
                existing.EndAt = eventItem.EndAt;

                Console.WriteLine($"[EventRepository] Событие #{eventItem.Id} обновлён в базе данных");
            
        }
    }

}

