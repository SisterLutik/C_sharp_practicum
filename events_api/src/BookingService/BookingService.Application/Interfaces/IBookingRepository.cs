using BookingService.Domain.Entities;
using BookingService.Domain.Enums;

namespace BookingService.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetByEventIdAsync(Guid eventId);
    Task<List<Booking>> GetPendingAsync();
    Task<List<Booking>> GetActiveByUserAsync(Guid userId);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}