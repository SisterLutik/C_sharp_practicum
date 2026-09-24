using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
        if (eventItem == null)
            return NotFound();

        return Ok(ToResponse(eventItem));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var newEvent = await _eventService.CreateEventAsync(
            request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);

        var response = ToResponse(newEvent);
        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateAsync(
            id, request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);

        return Ok(ToResponse(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// POST /api/events/{id}/book — создать бронь для события.
    /// </summary>
    [HttpPost("{id}/book")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(Guid id)
    {
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");

        var userId = Guid.Parse(userIdClaim);

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

    // =============================================
    // 🔧 Приватный маппинг Domain → DTO
    // =============================================
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