using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
        => await _eventRepository.GetByIdAsync(id);

    public async Task<PaginatedResult<Event>> GetAllAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1)
            throw new ValidationException("Page должен быть больше или равен 1");
        if (pageSize < 1)
            throw new ValidationException("PageSize должен быть больше или равен 1");
        if (from.HasValue && to.HasValue && from > to)
            throw new ValidationException("Дата начала (from) не может быть позже даты окончания (to)");

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
        _logger.LogInformation($"Создано событие {newEvent.Id}");
        return newEvent;
    }

    public async Task<Event> UpdateAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Событие с id {id} не найдено");

        if (endAt <= startAt)
            throw new ValidationException("EndAt должен быть позже StartAt");

        existingEvent.UpdateDetails(title, description, startAt, endAt, totalSeats);
        await _eventRepository.UpdateAsync(existingEvent);
        return existingEvent;
    }

    public async Task DeleteAsync(Guid id)
    {
        var exists = await _eventRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"Событие с id {id} не найдено");

        await _eventRepository.DeleteAsync(id);
    }
}