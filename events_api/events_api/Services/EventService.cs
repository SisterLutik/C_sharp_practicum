using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services

{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = new();
        public EventService()
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
                Id = 2,
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

        public void Delete (int id)
        {

            var eventItem = GetById(id);
            if (eventItem != null)
            {
                _events.Remove(eventItem);
                Console.WriteLine($"[EventService] Событие #{id} удалено");
            }
            else
            {
                Console.WriteLine($"[EventService] Событие #{id} не найдено");
            }

        }

        public void Update(int id, UpdateEventRequest request)
        {
            {
                var eventItem = GetById(id);
                if (eventItem == null)

                {
                    Console.WriteLine($"Событие #{id} не найдено");
                    return;
                }
                if (request.Title != null)
                    eventItem.Title = request.Title;

                if (request.Description != null)
                    eventItem.Description = request.Description;

                if (request.StartAt.HasValue)
                    eventItem.StartAt = request.StartAt.Value;

                if (request.EndAt.HasValue)
                    eventItem.EndAt = request.EndAt.Value;

                // Проверка EndAt >= StartAt
                if (eventItem.EndAt < eventItem.StartAt)
                    throw new InvalidOperationException("EndAt не может быть меньше StartAt");



                Console.WriteLine($"Событие #{eventItem.Id} успешно создано");
            }
        }


    }
}
        

    
