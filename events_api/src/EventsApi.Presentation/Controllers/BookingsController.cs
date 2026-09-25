using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    // GET /api/bookings/{id} — только аутентифицированные
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

    // DELETE /api/bookings/{id} — только аутентифицированные (владелец или админ)
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        await _bookingService.CancelBookingAsync(id, userId, role);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst("sub")?.Value
            ?? throw new ForbiddenOperationException("Не аутентифицирован");
        return Guid.Parse(claim);
    }

    private UserRole GetCurrentRole()
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        return Enum.Parse<UserRole>(claim);
    }
}