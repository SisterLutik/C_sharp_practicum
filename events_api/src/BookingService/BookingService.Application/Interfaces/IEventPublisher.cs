using EventsApi.Contracts.Kafka;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Абстракция для публикации событий в брокер сообщений.
/// Application не знает, что это Kafka — может быть RabbitMQ, Service Bus и т.д.
/// </summary>
public interface IEventPublisher
{
    Task PublishBookingConfirmedAsync(
        BookingConfirmed @event,
        CancellationToken cancellationToken = default);
}