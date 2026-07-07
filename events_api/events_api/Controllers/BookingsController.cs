using events_api.events_api.Exceptions;
using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using Microsoft.AspNetCore.Mvc;

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
        /// GET /bookings/{id} — получить бронь по идентификатору
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Booking), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
                throw new BusinessException($"Бронь с id {id} не найдена", 404);

            return Ok(booking);
        }
    }
}