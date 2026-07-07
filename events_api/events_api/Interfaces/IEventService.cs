using events_api.Models;

namespace events_api.Interfaces
{
    public interface IEventService
    {
        Event? GetById(Guid id);
        PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        void Add(Event eventItem);
        void Delete(Guid id);
        void Update(Guid id, Event updatedEvent);
    }
}
