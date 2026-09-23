using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
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
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        // booking == null не бывает: сервис бросает NotFoundException
        var response = new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        return Ok(response);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst("sub")?.Value
                                ?? throw new ForbiddenOperationException("Не аутентифицирован"));
        var role = Enum.Parse<UserRole>(HttpContext.User.FindFirst("role")?.Value ?? "User");

        await _bookingService.CancelBookingAsync(id, userId, role);
        return NoContent();
    }
}