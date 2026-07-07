using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

        public BookingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BookingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке броней");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("BookingBackgroundService остановлен");
        }

        private async Task ProcessPendingBookings(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            // Получаем все брони со статусом Pending
            var allBookings = bookingRepository.GetAll();
            var pendingBookings = allBookings.Where(b => b.Status == BookingStatus.Pending).ToList();

            if (!pendingBookings.Any())
                return;

            _logger.LogInformation($"Найдено {pendingBookings.Count} броней в статусе Pending");

            foreach (var booking in pendingBookings)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Имитация обращения к внешней системе (2 секунды)
                    _logger.LogInformation($"Обработка брони #{booking.Id}...");
                    await Task.Delay(2000, stoppingToken);

                    // Переводим бронь в статус Confirmed
                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.UtcNow;

                    // Сохраняем изменения
                    bookingRepository.Update(booking);

                    _logger.LogInformation($"Бронь #{booking.Id} подтверждена. ProcessedAt: {booking.ProcessedAt}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при обработке брони #{booking.Id}");
                }
            }
        }
    }
}
