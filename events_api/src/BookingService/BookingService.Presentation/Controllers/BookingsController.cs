using System.Security.Claims;
using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// GET /api/bookings/{id} — получить бронь по ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        });
    }

    /// <summary>
    /// POST /api/bookings/events/{eventId} — создать бронь для события.
    /// </summary>
    [HttpPost("events/{eventId}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(Guid eventId)
    {
        var userId = GetCurrentUserId();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        var response = new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, response);
    }

    /// <summary>
    /// DELETE /api/bookings/{id} — отменить бронь.
    /// Владелец может отменить только свою; админ — любую.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();

        await _bookingService.CancelBookingAsync(id, userId, isAdmin);
        return NoContent();
    }

    // ============================================
    // 🔧 Приватные помощники для чтения claims
    // ============================================

    private Guid GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");

        return Guid.Parse(claim);
    }

    private bool IsCurrentUserAdmin()
    {
        // В JWT у нас claim называется "role" (после MapInboundClaims = false)
        var role = HttpContext.User.FindFirst("role")?.Value;
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}