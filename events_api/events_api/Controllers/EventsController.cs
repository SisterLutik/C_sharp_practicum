using Microsoft.AspNetCore.Mvc;
using events_api.Interfaces;
using events_api.Models;

namespace events_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// GET /events — получить список всех событий с фильтрацией и пагинацией
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<Event>), 200)]
        [ProducesResponseType(400)]
        public IActionResult GetAll(
            [FromQuery] string? title,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Проверка: page не может быть меньше 1
            if (page < 1)
            {
                return BadRequest(new { Message = "Page должен быть больше или равен 1" });
            }

            // Проверка: pageSize не может быть меньше 1
            if (pageSize < 1)
            {
                return BadRequest(new { Message = "PageSize должен быть больше или равен 1" });
            }

            // Проверка: from не позже to
            if (from.HasValue && to.HasValue && from > to)
            {
                return BadRequest(new { Message = "Дата начала (from) не может быть позже даты окончания (to)" });
            }

            var result = _eventService.GetAll(title, from, to, page, pageSize);
            return Ok(result);
        }


        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var eventItem = _eventService.GetById(id);
            if (eventItem == null)
                return NotFound(new { Message = $"Событие с id {id} не найдено" });
            return Ok(eventItem);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Ошибка валидации модели", 400);

            if (request.EndAt <= request.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            var newEvent = new Event
            {
                Title = request.Title,
                Description = request.Description,
                StartAt = request.StartAt,
                EndAt = request.EndAt
            };

            _eventService.Add(newEvent);
            return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Event updatedEvent)
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
    }
}