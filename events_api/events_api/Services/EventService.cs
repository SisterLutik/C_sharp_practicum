using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.Data.Repositories;

namespace events_api.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<EventService> _logger;

        public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<Event?> GetByIdAsync(Guid id)
        {
            return await _eventRepository.GetByIdAsync(id);
        }

        public async Task<PaginatedResult<Event>> GetAllAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
        {
            return await _eventRepository.GetAllAsync(title, from, to, page, pageSize);
        }

        public async Task<Event> CreateEventAsync(
            string title,
            string? description,
            DateTime startAt,
            DateTime endAt,
            int totalSeats)
        {
            if (totalSeats <= 0)
                throw new BusinessException("TotalSeats должен быть больше 0", 400);

            if (endAt <= startAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            var newEvent = new Event(title, startAt, endAt, totalSeats, description);
            await _eventRepository.AddAsync(newEvent);

            _logger.LogInformation($"Создано событие {newEvent.Id}");
            return newEvent;
        }

        public async Task<Event> UpdateAsync(Guid id, Event updatedEvent)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(id);
            if (existingEvent == null)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            existingEvent.UpdateDetails(
                updatedEvent.Title,
                updatedEvent.Description,
                updatedEvent.StartAt,
                updatedEvent.EndAt,
                updatedEvent.TotalSeats
            );

            if (existingEvent.EndAt <= existingEvent.StartAt)
                throw new BusinessException("EndAt должен быть позже StartAt", 400);

            await _eventRepository.UpdateAsync(existingEvent);
            _logger.LogInformation($"Обновлено событие {id}");

            return existingEvent;
        }

        public async Task DeleteAsync(Guid id)
        {
            var exists = await _eventRepository.ExistsAsync(id);
            if (!exists)
                throw new BusinessException($"Событие с id {id} не найдено", 404);

            await _eventRepository.DeleteAsync(id);
            _logger.LogInformation($"Удалено событие {id}");
        }
    }
}