using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Exceptions;
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

    // GET /api/events/top — топ-10 по продажам
    [HttpGet("top")]
    [ProducesResponseType(typeof(List<EventResponse>), 200)]
    public async Task<IActionResult> GetTop()
    {
        var result = await _eventService.GetTopEventsAsync(10);
        return Ok(result);
    }

    // GET /api/events
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

    // GET /api/events/{id} — Cache-Aside
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);
        if (eventItem == null)
            throw new NotFoundException($"Событие с id {id} не найдено");

        return Ok(eventItem);   // ← уже EventResponse, не нужно мапить
    }

    // ... Create, Update, Delete, Reserve, Release — как раньше

    private static EventResponse ToResponse(Domain.Entities.Event e) => new()
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