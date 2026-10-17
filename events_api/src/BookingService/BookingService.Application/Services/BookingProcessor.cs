using BookingService.Application.Interfaces;
using BookingService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

public class BookingProcessor : IBookingProcessor
{
    private const int ExternalCallDelayMs = 2000;

    private readonly IBookingService _bookingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        IBookingService bookingService,
        IBookingRepository bookingRepository,
        ILogger<BookingProcessor> logger)
    {
        _bookingService = bookingService;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            // Имитация внешней системы — вынесено в константу
            await Task.Delay(ExternalCallDelayMs, stoppingToken);

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Pending)
            {
                _logger.LogInformation(
                    "Бронь {BookingId} не найдена или уже обработана", bookingId);
                return;
            }

            await _bookingService.ConfirmBookingAsync(bookingId);

            _logger.LogInformation("Бронь {BookingId} подтверждена", bookingId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Обработка брони {BookingId} отменена", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки брони {BookingId}", bookingId);
        }
    }
}