using events_api.events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult GetAll(
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

            var result = _eventService.GetAll(title, from, to, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);

            if (eventItem == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            var response = new EventResponse
            {
                Id = eventItem.Id,
                Title = eventItem.Title,
                Description = eventItem.Description,
                StartAt = eventItem.StartAt,
                EndAt = eventItem.EndAt,
                TotalSeats = eventItem.TotalSeats,
                AvailableSeats = eventItem.AvailableSeats
            };

            return Ok(response);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Ошибка валидации модели", 400);

            if (request.EndAt <= request.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            var newEvent = _eventService.CreateEvent(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats
           );

            var response = new EventResponse
            {
                Id = newEvent.Id,
                Title = newEvent.Title,
                Description = newEvent.Description,
                StartAt = newEvent.StartAt,
                EndAt = newEvent.EndAt,
                TotalSeats = newEvent.TotalSeats,
                AvailableSeats = newEvent.AvailableSeats
            };

            return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, response);
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] Event updatedEvent)
        {
            if (id != updatedEvent.Id)
                throw new BusinessException("Id в URL не совпадает с Id в теле запроса", 400);

            if (!ModelState.IsValid)
                throw new BusinessException("Ошибка валидации модели", 400);

            var existingEvent = _eventService.GetById(id);
            if (existingEvent == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            if (updatedEvent.EndAt <= updatedEvent.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            _eventService.Update(id, updatedEvent);
            return Ok(_eventService.GetById(id));
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            var existingEvent = _eventService.GetById(id);
            if (existingEvent == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            _eventService.Delete(id);
            return NoContent();
        }
        [HttpPost("{id}/book")]
               [ProducesResponseType(typeof(Booking), 202)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            // Проверяем, существует ли событие
            var eventExists = _eventService.GetById(id);
            if (eventExists == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            // Создаём бронь
            var booking = await _bookingService.CreateBookingAsync(id);

            // Возвращаем 202 Accepted с телом и Location
            return Accepted(
                new Uri($"/api/bookings/{booking.Id}", UriKind.Relative),
                booking
            );
        }

    }
}