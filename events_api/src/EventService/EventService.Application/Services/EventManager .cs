using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Services;

public class EventManager : IEventService
{
    private const int EventCacheTtlMinutes = 5;
    private const int TopCacheTtlMinutes = 10;
    private const string TopEventsCacheKey = "events:top10";

    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<EventManager> _logger;

    public EventManager(
        IEventRepository eventRepository,
        ICacheService cache,
        ILogger<EventManager> logger)
    {
        _eventRepository = eventRepository;
        _cache = cache;
        _logger = logger;
    }

    // Cache-Aside: GET /events/{id}
    public async Task<EventResponse?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"event:{id}";

        // 1. Пробуем из кеша
        var cached = await _cache.GetAsync<EventResponse>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Событие {EventId} получено из кеша", id);
            return cached;
        }

        // 2. Cache miss — идём в БД
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem == null)
            return null;

        // 3. Кладём в кеш с TTL
        var response = ToResponse(eventItem);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(EventCacheTtlMinutes));

        _logger.LogDebug("Событие {EventId} загружено из БД и закешировано", id);
        return response;
    }

    //  Cache-Aside: GET /events/top
    public async Task<List<EventResponse>> GetTopEventsAsync(int count = 10)
    {
        // 1. Пробуем из кеша
        var cached = await _cache.GetAsync<List<EventResponse>>(TopEventsCacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Топ-{Count} событий получен из кеша", count);
            return cached;
        }

        // 2. Cache miss — идём в БД
        var top = await _eventRepository.GetTopBySalesPercentageAsync(count);
        var response = top.Select(ToResponse).ToList();

        // 3. Кладём в кеш с TTL
        await _cache.SetAsync(
            TopEventsCacheKey,
            response,
            TimeSpan.FromMinutes(TopCacheTtlMinutes));

        _logger.LogDebug("Топ-{Count} событий загружен из БД и закеширован", count);
        return response;
    }

    // Остальные методы — без кеша

    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1)
            throw new ValidationException("Page должен быть больше или равен 1");
        if (pageSize < 1)
            throw new ValidationException("PageSize должен быть больше или равен 1");
        if (from.HasValue && to.HasValue && from > to)
            throw new ValidationException("Дата from не может быть позже to");

        return await _eventRepository.GetAllAsync(title, from, to, page, pageSize);
    }

    public async Task<Event> CreateEventAsync(
        string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (totalSeats <= 0)
            throw new ValidationException("TotalSeats должен быть больше 0");
        if (endAt <= startAt)
            throw new ValidationException("EndAt должен быть позже StartAt");

        var newEvent = new Event(title, startAt, endAt, totalSeats, description);
        await _eventRepository.AddAsync(newEvent);

        _logger.LogInformation("Создано событие {EventId}", newEvent.Id);
        return newEvent;
    }

    public async Task<Event> UpdateAsync(
        Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Событие с id {id} не найдено");

        if (endAt <= startAt)
            throw new ValidationException("EndAt должен быть позже StartAt");

        existingEvent.UpdateDetails(title, description, startAt, endAt, totalSeats);
        await _eventRepository.UpdateAsync(existingEvent);

        // Инвалидация кеша — ЭТАП 5. Пока не реализуем, но место обозначено.
        // await _cache.RemoveAsync($"event:{id}");

        _logger.LogInformation("Обновлено событие {EventId}", id);
        return existingEvent;
    }

    public async Task DeleteAsync(Guid id)
    {
        var exists = await _eventRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"Событие с id {id} не найдено");

        await _eventRepository.DeleteAsync(id);

        // Инвалидация кеша — ЭТАП 5.
        // await _cache.RemoveAsync($"event:{id}");

        _logger.LogInformation("Удалено событие {EventId}", id);
    }

    // Маппинг Domain → DTO
    private static EventResponse ToResponse(Event e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        StartAt = e.StartAt,
        EndAt = e.EndAt,
        TotalSeats = e.TotalSeats,
        AvailableSeats = e.AvailableSeats
    };
}