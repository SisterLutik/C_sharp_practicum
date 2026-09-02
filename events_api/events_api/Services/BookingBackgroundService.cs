using events_api.Data.Repositories;
using events_api.Models;


namespace events_api.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

        public BookingBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingBackgroundService запущен");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingBookings(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Ошибка при обработке броней");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("BookingBackgroundService остановлен");
        }

        private async Task ProcessPendingBookings(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var pendingBookings = await bookingRepository.GetPendingAsync();

            if (!pendingBookings.Any())
                return;

            _logger.LogInformation($"Найдено {pendingBookings.Count} броней в статусе Pending");

            var tasks = pendingBookings.Select(booking =>
                ProcessBookingWithOwnScopeAsync(booking, stoppingToken));

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingWithOwnScopeAsync(Booking booking, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            try
            {
                _logger.LogInformation($"Обработка брони #{booking.Id}...");
                await Task.Delay(2000, stoppingToken);

                var bookingEntity = await bookingRepository.GetByIdAsync(booking.Id);
                if (bookingEntity == null || bookingEntity.Status != BookingStatus.Pending)
                    return;

                var eventExists = await eventRepository.GetByIdAsync(bookingEntity.EventId);
                if (eventExists == null)
                {
                    bookingEntity.Reject();
                    await bookingRepository.UpdateAsync(bookingEntity);
                    _logger.LogWarning($"Бронь #{booking.Id} отклонена: событие не найдено");
                    return;
                }

                bookingEntity.Confirm();
                await bookingRepository.UpdateAsync(bookingEntity);
                _logger.LogInformation($"Бронь #{booking.Id} подтверждена");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Обработка брони #{booking.Id} отменена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке брони #{booking.Id}");
            }
        }
    }
}