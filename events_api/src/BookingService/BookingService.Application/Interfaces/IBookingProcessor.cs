namespace BookingService.Application.Interfaces;

/// <summary>
/// Обработчик одной брони — используется фоновым сервисом.
/// Подтверждает или отклоняет «висящую» бронь в статусе Pending.
/// </summary>
public interface IBookingProcessor
{
    Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken);
}