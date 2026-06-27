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

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_eventService.GetAll());
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
                return BadRequest(ModelState);

            if (request.EndAt <= request.StartAt)
                return BadRequest(new { Message = "EndAt должен быть позже StartAt" });

            // Маппинг DTO → модель
            var newEvent = new Event
            {
                Title = request.Title,
                Description = request.Description,
                StartAt = request.StartAt,
                EndAt = request.EndAt
            };

            _eventService.Add(newEvent); // Id генерируется внутри сервиса

            return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
        }


        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Event updatedEvent)
        {
            if (id != updatedEvent.Id)
                return BadRequest(new { Message = "Id в URL не совпадает с Id в теле запроса" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingEvent = _eventService.GetById(id);
            if (existingEvent == null)
                return NotFound(new { Message = $"Событие с id {id} не найдено" });

            if (updatedEvent.EndAt <= updatedEvent.StartAt)
                return BadRequest(new { Message = "EndAt должен быть позже StartAt" });

            _eventService.Update(id, updatedEvent);
            return Ok(_eventService.GetById(id));
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var existingEvent = _eventService.GetById(id);
            if (existingEvent == null)
                return NotFound(new { Message = $"Событие с id {id} не найдено" });

            _eventService.Delete(id);
            return NoContent();
        }
    }
}