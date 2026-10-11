using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using EventsApi.Contracts.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // ============================================
    // 📖 Публичные эндпоинты (без авторизации)
    // ============================================

    /// <summary>
    /// GET /api/events — список событий с фильтрацией и пагинацией.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventResponse>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _eventService.GetAllAsync(title, from, to, page, pageSize);

        return Ok(new
        {
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            Items = result.Items.Select(ToResponse)
        });
    }

    /// <summary>
    /// GET /api/events/{id} — получить событие по ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);
        if (eventItem == null)
            throw new NotFoundException($"Событие с id {id} не найдено");

        return Ok(ToResponse(eventItem));
    }

    // ============================================
    // 🔒 Admin-only эндпоинты
    // ============================================

    /// <summary>
    /// POST /api/events — создать событие (только Admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var newEvent = await _eventService.CreateEventAsync(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        var response = ToResponse(newEvent);
        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, response);
    }

    /// <summary>
    /// PUT /api/events/{id} — обновить событие (только Admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateAsync(
            id,
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        return Ok(ToResponse(updated));
    }

    /// <summary>
    /// DELETE /api/events/{id} — удалить событие (только Admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    // ============================================
    // 🔗 Межсервисные эндпоинты (для BookingService)
    // ============================================

    /// <summary>
    /// POST /api/events/{id}/reserve — зарезервировать одно место.
    /// Вызывается BookingService при создании брони.
    /// </summary>
    [HttpPost("{id}/reserve")]
    [ProducesResponseType(typeof(ReserveSeatsResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ReserveSeats(Guid id)
    {
        var result = await _eventService.TryReserveSeatsAsync(id);

        if (!result.Success && result.Reason == "Event not found")
            throw new NotFoundException($"Событие с id {id} не найдено");

        if (!result.Success)
            return Conflict(new
            {
                Message = result.Reason ?? "No available seats for this event",
                AvailableSeats = result.AvailableSeats
            });

        return Ok(result);
    }

    /// <summary>
    /// POST /api/events/{id}/release — вернуть одно место.
    /// Вызывается BookingService при отмене брони.
    /// </summary>
    [HttpPost("{id}/release")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReleaseSeats(Guid id)
    {
        var success = await _eventService.ReleaseSeatsAsync(id);

        if (!success)
            throw new NotFoundException($"Событие с id {id} не найдено");

        return NoContent();
    }

    // ============================================
    // 🔧 Приватный маппинг Domain → DTO
    // ============================================

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