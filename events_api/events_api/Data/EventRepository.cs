using events_api.Interfaces;
using events_api.Models;

namespace events_api.Data

{
    public class EventRepository : IEventRepository
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

        public bool Update(int id, UpdateEventRequest request, out List<string> errors)
        {
            errors = new List<string>();
            var existingEvent = GetById(id);

            if (existingEvent == null)
            {
                errors.Add("Событие не найдено");
                return false;
            }

            // Обновляем Title (если передан)
            if (request.Title != null)
            {
                existingEvent.Title = request.Title;
            }

            // Обновляем Description (если передан — даже пустая строка)
            if (request.Description != null)
            {
                existingEvent.Description = request.Description;
            }

            // Обновляем StartAt (если передан)
            if (request.StartAt.HasValue)
            {
                existingEvent.StartAt = request.StartAt.Value;
            }

            // Обновляем EndAt (если передан)
            if (request.EndAt.HasValue)
            {
                existingEvent.EndAt = request.EndAt.Value;
            }

            // Финальная проверка: EndAt не может быть меньше StartAt
            if (existingEvent.EndAt < existingEvent.StartAt)
            {
                errors.Add("EndAt не может быть меньше StartAt");
                return false;
            }

            return true;
        }


    }
        

    
