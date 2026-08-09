using Microsoft.AspNetCore.Mvc;
using events_api.Interfaces;
using events_api.Models;
using events_api.Exceptions;

namespace events_api.Controllers
{
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
        [ProducesResponseType(typeof(PaginatedResult<Event>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? title,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1)
                throw new BusinessException("Page должен быть больше или равен 1", 400);

            if (pageSize < 1)
                throw new BusinessException("PageSize должен быть больше или равен 1", 400);

            if (from.HasValue && to.HasValue && from > to)
                throw new BusinessException("Дата начала (from) не может быть позже даты окончания (to)", 400);

            // ✅ Используем GetAllAsync
            var result = await _eventService.GetAllAsync(title, from, to, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // ✅ Используем GetByIdAsync
            var eventItem = await _eventService.GetByIdAsync(id);

            if (eventItem == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            return Ok(eventItem);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Event), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Ошибка валидации модели", 400);

            if (request.EndAt <= request.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            // ✅ Используем CreateEventAsync (уже правильно)
            var newEvent = await _eventService.CreateEventAsync(
                request.Title,
                request.Description,
                request.StartAt,
                request.EndAt,
                request.TotalSeats
            );

            return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] Event updatedEvent)
        {
            if (id != updatedEvent.Id)
                throw new BusinessException("Id в URL не совпадает с Id в теле запроса", 400);

            if (!ModelState.IsValid)
                throw new BusinessException("Ошибка валидации модели", 400);

            // ✅ Используем UpdateAsync
            var result = await _eventService.UpdateAsync(id, updatedEvent);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // ✅ Используем DeleteAsync
            await _eventService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/book")]
        [ProducesResponseType(typeof(BookingResponse), 202)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            // ✅ Используем GetByIdAsync
            var eventExists = await _eventService.GetByIdAsync(id);
            if (eventExists == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            // ✅ Используем CreateBookingAsync
            var booking = await _bookingService.CreateBookingAsync(id);

            var response = new BookingResponse
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };

            return Accepted(
                new Uri($"/api/bookings/{booking.Id}", UriKind.Relative),
                response
            );
        }
    }
}