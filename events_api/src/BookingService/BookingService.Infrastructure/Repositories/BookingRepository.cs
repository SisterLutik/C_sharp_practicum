using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Booking>> GetByEventIdAsync(Guid eventId)
    {
        return await _context.Bookings
            .Where(b => b.EventId == eventId)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetPendingAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetActiveByUserAsync(Guid userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId
                     && (b.Status == BookingStatus.Pending
                         || b.Status == BookingStatus.Confirmed))
            .ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);   

        if (booking != null)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Bookings.AnyAsync(b => b.Id == id);
    }
}