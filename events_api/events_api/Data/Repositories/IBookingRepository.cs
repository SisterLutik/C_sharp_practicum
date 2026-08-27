using events_api.Models;

namespace events_api.Data.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(Guid id);
        Task<List<Booking>> GetByEventIdAsync(Guid eventId);
        Task<List<Booking>> GetPendingAsync();
        Task AddAsync(Booking booking);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}