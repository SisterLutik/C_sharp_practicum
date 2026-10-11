using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _context;

    public EventRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var lowerTitle = title.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(lowerTitle));
        }

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Event>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task AddAsync(Event eventItem)
    {
        await _context.Events.AddAsync(eventItem);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event eventItem)
    {
        _context.Events.Update(eventItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem != null)
        {
            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Events.AnyAsync(e => e.Id == id);
    }
}