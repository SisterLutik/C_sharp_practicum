using Microsoft.AspNetCore.Mvc;
using events_api.Interfaces;
using events_api.Models;
using events_api.Exceptions;

namespace events_api.Controllers
{
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
        /// GET /api/bookings/{id} — получить бронь по ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookingResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // ✅ Используем правильное имя метода
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
                throw new BusinessException($"Бронь с id {id} не найдена", 404);

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
}