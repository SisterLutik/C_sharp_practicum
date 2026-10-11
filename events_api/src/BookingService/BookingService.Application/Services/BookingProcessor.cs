using BookingService.Application.Interfaces;
using BookingService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

public class BookingProcessor : IBookingProcessor
{
    private readonly IBookingService _bookingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        IBookingService bookingService,
        IBookingRepository bookingRepository,
        IEventServiceClient eventServiceClient,
        ILogger<BookingProcessor> logger)
    {
        _bookingService = bookingService;
        _bookingRepository = bookingRepository;
        _eventServiceClient = eventServiceClient;
        _logger = logger;
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            // Имитация обращения к внешней системе
            await Task.Delay(2000, stoppingToken);

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Pending)
            {
                _logger.LogInformation(
                    "Бронь {BookingId} не найдена или уже обработана", bookingId);
                return;
            }

            var eventDto = await _eventServiceClient.GetEventAsync(booking.EventId);
            if (eventDto == null)
            {
                booking.Reject();
                await _bookingRepository.UpdateAsync(booking);
                _logger.LogWarning(
                    "Бронь {BookingId} отклонена: событие не найдено", bookingId);
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