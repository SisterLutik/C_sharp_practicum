namespace BookingService.Application.Interfaces;

public interface IBookingProcessor
{
    Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken);
}