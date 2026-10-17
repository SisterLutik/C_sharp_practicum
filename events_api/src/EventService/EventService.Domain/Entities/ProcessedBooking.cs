namespace EventService.Domain.Entities;

/// <summary>
/// Отметка об уже обработанном сообщении BookingConfirmed.
/// Защищает от повторного уменьшения мест при повторной доставке из Kafka.
/// </summary>
public class ProcessedBooking
{
    private ProcessedBooking() { }

    public ProcessedBooking(Guid bookingId)
    {
        BookingId = bookingId;
        ProcessedAt = DateTime.UtcNow;
    }

    public Guid BookingId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
}