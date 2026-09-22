using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
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
}