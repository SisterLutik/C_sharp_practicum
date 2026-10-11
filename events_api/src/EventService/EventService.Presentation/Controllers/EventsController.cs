using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using EventsApi.Contracts.Events;
using Microsoft.AspNetCore.Authorization;   // ← для [Authorize]
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

    // Публичный
    [HttpGet]
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

    // Публичный
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);
        if (eventItem == null)
            throw new NotFoundException($"Событие с id {id} не найдено");
        return Ok(ToResponse(eventItem));
    }

    // 🔒 Admin-only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var newEvent = await _eventService.CreateEventAsync(
            request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, ToResponse(newEvent));
    }

    // 🔒 Admin-only
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateAsync(
            id, request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
        return Ok(ToResponse(updated));
    }

    // 🔒 Admin-only
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    // 🔗 Межсервисные — внутренние, без [Authorize]
    // (в продакшене защищаются service-to-service токеном или сетью)
    [HttpPost("{id}/reserve")]
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

    [HttpPost("{id}/release")]
    public async Task<IActionResult> ReleaseSeats(Guid id)
    {
        var success = await _eventService.ReleaseSeatsAsync(id);
        if (!success)
            throw new NotFoundException($"Событие с id {id} не найдено");
        return NoContent();
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