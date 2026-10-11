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

    // 🔒 Требует аутентификации
    [HttpGet("{id}")]
    [Authorize]
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

    // 🔒 Требует аутентификации
    [HttpPost("events/{eventId}")]
    [Authorize]
    public async Task<IActionResult> Create(Guid eventId)
    {
        var userId = GetCurrentUserId();
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        });
    }

    // 🔒 Требует аутентификации (владелец или админ)
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();

        await _bookingService.CancelBookingAsync(id, userId, isAdmin);
        return NoContent();
    }

    // ============================================
    // 🔧 Помощники
    // ============================================

    private Guid GetCurrentUserId()
    {
        // ✅ С MapInboundClaims=false claim остаётся "sub"
        var claim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");
        return Guid.Parse(claim);
    }

    private bool IsCurrentUserAdmin()
    {
        // ✅ С MapInboundClaims=false и RoleClaimType="role" читаем короткое имя
        var role = HttpContext.User.FindFirst("role")?.Value;
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}