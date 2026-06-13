using events_api.Interfaces;
using events_api.Models;
using System.Security.Cryptography.X509Certificates;

namespace events_api.Services
{
    public class EventService
    {
        private readonly IEventRepository _repository;

        // Конструктор принимает интерфейсы, а не конкретные реализации
        public EventService(IEventRepository repository)
        {
            _repository = repository;
        }

        public Event? GetEvent(int id)
        {
            return _repository.GetById(id);
        }

        public List<Event> GetAllEvents()
        {
            return _repository.GetAll();
        }

        public void CreateEvent(string title, string description, DateTime startAt, DateTime endAt)
        {
            var eventItem = new Event
            {
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt
            };

            _repository.Add(eventItem);

            Console.WriteLine($"Событие #{eventItem.Id} успешно создано");
        }

        public void UpdateEvent(int EventId, UpdateEventRequest request)
        {
            {
                var eventItem = _repository.GetById(EventId);
                if (eventItem == null)

                {
                    Console.WriteLine($"Событие #{EventId} не найдено");
                    return;
                }
                if (eventItem.Title != null)
                    eventItem.Title = request.Title;

                if (request.Description != null)
                    existingEvent.Description = request.Description;

                if (request.StartAt.HasValue)
                    existingEvent.StartAt = request.StartAt.Value;

                if (request.EndAt.HasValue)
                    existingEvent.EndAt = request.EndAt.Value;

                // Проверка EndAt >= StartAt
                if (existingEvent.EndAt < existingEvent.StartAt)
                    throw new InvalidOperationException("EndAt не может быть меньше StartAt");
            }


            Console.WriteLine($"Событие #{eventItem.Id} успешно создано");
        }

    }
}


