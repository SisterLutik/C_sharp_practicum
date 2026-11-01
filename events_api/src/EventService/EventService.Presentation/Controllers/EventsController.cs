using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Application.Services;
using EventService.Domain.Entities;
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

    [HttpGet("top")]
    [ProducesResponseType(typeof(List<EventResponse>), 200)]
    public async Task<IActionResult> GetTop()
    {
        var result = await _eventService.GetTopEventsAsync(10);
        return Ok(result);
    }

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
            Items = result.Items.Select(EventManager.ToResponse)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);
        if (eventItem == null)
            throw new NotFoundException($"Событие с id {id} не найдено");

        return Ok(eventItem);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var newEvent = await _eventService.CreateEventAsync(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        return CreatedAtAction(
            nameof(GetById),
            new { id = newEvent.Id },
            EventManager.ToResponse(newEvent));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateAsync(
            id,
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        return Ok(EventManager.ToResponse(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }
}