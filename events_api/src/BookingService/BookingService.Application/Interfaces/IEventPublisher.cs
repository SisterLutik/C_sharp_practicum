using EventsApi.Contracts.Kafka;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Абстракция для публикации событий в брокер сообщений.
/// Application-слой не знает, что это Kafka — может быть RabbitMQ, Service Bus и т.д.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикует событие BookingConfirmed в топик.
    /// </summary>
    Task PublishBookingConfirmedAsync(BookingConfirmed @event, CancellationToken cancellationToken = default);
}