using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Application.Settings;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Application.Services;

public class EventManager : IEventService
{
    private const string TopEventsCacheKey = "events:top10";

    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<EventManager> _logger;

    public EventManager(
        IEventRepository eventRepository,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings,
        ILogger<EventManager> logger)
    {
        _eventRepository = eventRepository;
        _cache = cache;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"event:{id}";

        var cached = await _cache.GetAsync<EventResponse>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Событие {EventId} получено из кеша", id);
            return cached;
        }

        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem == null)
            return null;

        var response = ToResponse(eventItem);
        await _cache.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(_cacheSettings.EventTtlMinutes));

        _logger.LogDebug("Событие {EventId} загружено из БД и закешировано", id);
        return response;
    }

    public async Task<List<EventResponse>> GetTopEventsAsync(int count = 10)
    {
        var cached = await _cache.GetAsync<List<EventResponse>>(TopEventsCacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Топ-{Count} событий получен из кеша", count);
            return cached;
        }

        var top = await _eventRepository.GetTopBySalesPercentageAsync(count);
        var response = top.Select(ToResponse).ToList();

        await _cache.SetAsync(
            TopEventsCacheKey,
            response,
            TimeSpan.FromMinutes(_cacheSettings.TopEventsTtlMinutes));

        _logger.LogDebug("Топ-{Count} событий загружен из БД и закеширован", count);
        return response;
    }

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

        await _cache.RemoveAsync($"event:{id}");

        _logger.LogInformation("Обновлено событие {EventId}, кеш инвалидирован", id);
        return existingEvent;
    }

    public async Task DeleteAsync(Guid id)
    {
        var exists = await _eventRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"Событие с id {id} не найдено");

        await _eventRepository.DeleteAsync(id);

        await _cache.RemoveAsync($"event:{id}");

        _logger.LogInformation("Удалено событие {EventId}, кеш инвалидирован", id);
    }

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