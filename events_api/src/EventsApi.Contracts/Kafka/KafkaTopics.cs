namespace EventsApi.Contracts.Kafka;

/// <summary>
/// Имена Kafka-топиков.
/// Издатель и подписчик ссылаются на одну и ту же константу,
/// чтобы не было расхождений.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Топик, в который BookingService публикует событие подтверждения брони.
    /// EventService подписывается на него и уменьшает доступные места.
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}