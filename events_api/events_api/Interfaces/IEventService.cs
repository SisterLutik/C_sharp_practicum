using events_api.Models;

namespace events_api.Interfaces
{
    public interface IEventService
    {
        Event? GetById(int id);
        PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        void Add(Event eventItem);
        void Delete(int id);
        void Update(int id, Event updatedEvent);
    }
}
