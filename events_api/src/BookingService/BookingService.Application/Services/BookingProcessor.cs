using BookingService.Application.Interfaces;
using BookingService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

public class BookingProcessor : IBookingProcessor
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        IBookingRepository bookingRepository,
        ILogger<BookingProcessor> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;
            booking.Confirm();
            await _bookingRepository.UpdateAsync(booking);
            _logger.LogInformation($"Бронь {booking.Id} подтверждена");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"Обработка брони {bookingId} отменена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка обработки брони {bookingId}");
        }
    }
}