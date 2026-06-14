using events_api.Models;

namespace events_api.Interfaces
{
    public interface IEventService
    {
        Event? GetById(int id);
        List<Event> GetAll();
        void Add(Event eventItem);
        void Delete(int id);
        void Update(int id, UpdateEventRequest request);
        }
}
