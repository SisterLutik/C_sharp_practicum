using events_api.Interfaces;
using events_api.Models;

namespace events_api.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

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

            var tasks = pendingBookings.Select(booking =>
                ProcessBookingAsync(booking, stoppingToken, scope));

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(
            Booking booking,
            CancellationToken stoppingToken,
            IServiceScope scope)
        {
            try
            {
                // 1. Имитация обращения к внешней системе (параллельно у всех броней)
                _logger.LogInformation($"Обработка брони #{booking.Id}...");
                await Task.Delay(2000, stoppingToken);

                // 2. Захват семафора перед записью в хранилище
                await _processingSemaphore.WaitAsync(stoppingToken);

                try
                {
                    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                    // 3. Проверяем, существует ли событие
                    var eventExists = eventService.GetById(booking.EventId);

                    if (eventExists == null)
                    {
                        // Событие удалено — отклоняем бронь
                        booking.Reject();
                        bookingRepository.Update(booking);
                        _logger.LogWarning($"Бронь #{booking.Id} отклонена: событие #{booking.EventId} не найдено");
                        return;
                    }

                    // 4. Подтверждаем бронь
                    booking.Confirm();
                    bookingRepository.Update(booking);
                    _logger.LogInformation($"Бронь #{booking.Id} подтверждена. ProcessedAt: {booking.ProcessedAt}");
                }
                finally
                {
                    // 5. Освобождаем семафор
                    _processingSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Обработка брони #{booking.Id} отменена");
            }
            catch (Exception ex)
            {
                // 6. Непредвиденная ошибка — отклоняем бронь и возвращаем место
                try
                {
                    await _processingSemaphore.WaitAsync(stoppingToken);

                    using var scopeFallback = _serviceProvider.CreateScope();
                    var eventService = scopeFallback.ServiceProvider.GetRequiredService<IEventService>();
                    var bookingRepository = scopeFallback.ServiceProvider.GetRequiredService<IBookingRepository>();

                    var eventExists = eventService.GetById(booking.EventId);
                    if (eventExists != null)
                    {
                        eventExists.ReleaseSeats();
                        eventService.Update(booking.EventId, eventExists);
                    }

                    booking.Reject();
                    bookingRepository.Update(booking);

                    _logger.LogError(ex, $"Бронь #{booking.Id} отклонена из-за ошибки");
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, $"Критическая ошибка при обработке брони #{booking.Id}");
                }
                finally
                {
                    if (_processingSemaphore.CurrentCount == 0)
                        _processingSemaphore.Release();
                }
            }
        }
    }
}