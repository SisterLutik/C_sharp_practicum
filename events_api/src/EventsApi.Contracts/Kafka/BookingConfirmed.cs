namespace EventsApi.Contracts.Kafka;

/// <summary>
/// Контракт сообщения о подтверждении брони.
/// </summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAt);