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
        /// GET /events — получить список всех событий
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<Event>), 200)]
        public IActionResult GetAll()
        {
            var events = _eventService.GetAll();
            return Ok(events);
        }

        /// <summary>
        /// GET /events/{id} — получить событие по id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id)
        {
            var eventItem = _eventService.GetById(id);

            if (eventItem == null)
                return NotFound(new { Message = $"Событие с id {id} не найдено" });

            return Ok(eventItem);
        }

        /// <summary>
        /// POST /events — создать событие
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Event), 201)]
        [ProducesResponseType(400)]
        public IActionResult Create([FromBody] Event newEvent)
        {
            // Валидация модели
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Дополнительная проверка EndAt > StartAt
            if (newEvent.EndAt <= newEvent.StartAt)
            {
                return BadRequest(new { Message = "EndAt должен быть позже StartAt" });
            }

            _eventService.Add(newEvent);

            return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
        }

        /// <summary>
        /// PATCH /events/{id} — обновить событие (частично или полностью)
        /// </summary>
        [HttpPatch("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Update(int id, [FromBody] UpdateEventRequest request)
        {
            if (request == null)
                return BadRequest(new { Error = "Request body is required" });

            // Получаем событие до обновления
            var existingEvent = _eventService.GetById(id);
            if (existingEvent == null)
                return NotFound(new { Message = $"Событие с id {id} не найдено" });

            // Сохраняем старые значения для отслеживания изменений
            var updatedFields = new List<string>();

            // Обновляем поля
            if (request.Title != null)
            {
                existingEvent.Title = request.Title;
                updatedFields.Add("Title");
            }

            if (request.Description != null)
            {
                existingEvent.Description = request.Description;
                updatedFields.Add("Description");
            }

            if (request.StartAt.HasValue)
            {
                existingEvent.StartAt = request.StartAt.Value;
                updatedFields.Add("StartAt");
            }

            if (request.EndAt.HasValue)
            {
                existingEvent.EndAt = request.EndAt.Value;
                updatedFields.Add("EndAt");
            }

            // Проверка EndAt > StartAt
            if (existingEvent.EndAt <= existingEvent.StartAt)
            {
                return BadRequest(new { Message = "EndAt должен быть позже StartAt" });
            }

            // Вызываем сервис для сохранения изменений
            _eventService.Update(id, request);

            // Выводим информацию в консоль (для отладки)
            Console.WriteLine($"Обновлено событие с Id {id}");
            Console.WriteLine($"Изменённые поля: {string.Join(", ", updatedFields)}");
            Console.WriteLine($"Текущее состояние: Title = {existingEvent.Title}, StartAt = {existingEvent.StartAt}, EndAt = {existingEvent.EndAt}");

            // Возвращаем обновлённое событие + информацию о том, что изменилось
            return Ok(new
            {
                Message = "Событие успешно обновлено",
                UpdatedFields = updatedFields,
                Event = existingEvent
            });
        }

        /// <summary>
        /// DELETE /events/{id} — удалить событие
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
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