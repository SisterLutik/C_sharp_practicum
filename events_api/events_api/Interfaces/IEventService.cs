using events_api.Models;

namespace events_api.Interfaces
{
    public interface IEventService
    {
        Event? GetById(int id);
        List<Event> GetAll(string? title, DateTime? from, DateTime? to);
        void Add(Event eventItem);
        void Delete(int id);
        void Update(int id, Event updatedEvent);
    }
}
