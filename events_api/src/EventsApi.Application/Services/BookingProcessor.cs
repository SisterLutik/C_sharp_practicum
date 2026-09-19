using EventsApi.Application.Interfaces;
using EventsApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;

public class BookingProcessor : IBookingProcessor
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ILogger<BookingProcessor> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
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

            var eventExists = await _eventRepository.GetByIdAsync(booking.EventId);
            if (eventExists == null)
            {
                booking.Reject();
                await _bookingRepository.UpdateAsync(booking);
                _logger.LogWarning($"Бронь {booking.Id} отклонена: событие не найдено");
                return;
            }

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