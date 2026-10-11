using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using EventsApi.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Services;

public class EventManager : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventManager> _logger;

    public EventManager(IEventRepository eventRepository, ILogger<EventManager> logger)
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

        _logger.LogInformation($"Создано событие {newEvent.Id}");
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

        _logger.LogInformation($"Обновлено событие {id}");
        return existingEvent;
    }

    public async Task DeleteAsync(Guid id)
    {
        var exists = await _eventRepository.ExistsAsync(id);
        if (!exists)
            throw new NotFoundException($"Событие с id {id} не найдено");

        await _eventRepository.DeleteAsync(id);
        _logger.LogInformation($"Удалено событие {id}");
    }

    // ============================================
    // 🔗 Межсервисные методы (для BookingService)
    // ============================================

    public async Task<ReserveSeatsResponse> TryReserveSeatsAsync(Guid eventId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null)
        {
            return new ReserveSeatsResponse
            {
                Success = false,
                Reason = "Event not found",
                AvailableSeats = 0
            };
        }

        if (!eventEntity.TryReserveSeats())
        {
            return new ReserveSeatsResponse
            {
                Success = false,
                Reason = "No available seats for this event",
                AvailableSeats = eventEntity.AvailableSeats
            };
        }

        await _eventRepository.UpdateAsync(eventEntity);

        _logger.LogInformation(
            $"Зарезервировано место для события {eventId}. Осталось: {eventEntity.AvailableSeats}");

        return new ReserveSeatsResponse
        {
            Success = true,
            AvailableSeats = eventEntity.AvailableSeats
        };
    }

    public async Task<bool> ReleaseSeatsAsync(Guid eventId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
            return false;

        eventEntity.ReleaseSeats();
        await _eventRepository.UpdateAsync(eventEntity);

        _logger.LogInformation(
            $"Возвращено место для события {eventId}. Доступно: {eventEntity.AvailableSeats}");

        return true;
    }
}