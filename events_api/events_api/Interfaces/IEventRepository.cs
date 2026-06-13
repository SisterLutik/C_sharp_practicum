using events_api.Models;

namespace events_api.Interfaces
{
    public interface IEventRepository
    {
        Event? GetById(int id);
        List<Event> GetAll();
        void Add(Event eventItem);
        bool Update(int id, UpdateEventRequest request, out List<string> errors);
        }
}
