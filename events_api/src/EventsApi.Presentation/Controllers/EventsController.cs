using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    // GET /api/events — доступен всем
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);
        if (eventItem == null) return NotFound();
        return Ok(ToResponse(eventItem));
    }

    // POST /api/events — только Admin
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var newEvent = await _eventService.CreateEventAsync(
            request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);

        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, ToResponse(newEvent));
    }

    // PUT /api/events/{id} — только Admin
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateAsync(
            id, request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);

        return Ok(ToResponse(updated));
    }

    // DELETE /api/events/{id} — только Admin
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    // POST /api/events/{id}/book — только аутентифицированные
    [HttpPost("{id}/book")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(Guid id)
    {
        var userId = GetCurrentUserId();
        var booking = await _bookingService.CreateBookingAsync(id, userId);

        var response = new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        return Accepted(new Uri($"/api/bookings/{booking.Id}", UriKind.Relative), response);
    }

    private Guid GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");
        return Guid.Parse(claim);
    }

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