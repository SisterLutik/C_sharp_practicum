using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

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

    /// <summary>
    /// POST /api/bookings/{id}/cancel — отменить бронь.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");
        var roleClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

        var userId = Guid.Parse(userIdClaim);
        var role = Enum.Parse<UserRole>(roleClaim);

        await _bookingService.CancelBookingAsync(id, userId, role);
        return NoContent();
    }
}