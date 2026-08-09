using events_api.Interfaces;
using events_api.Models;
using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            // ✅ Создаём scope для чтения Pending броней
            using var readScope = _scopeFactory.CreateScope();
            var dbContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingBookingIds = await dbContext.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .Select(b => b.Id)
                .ToListAsync(stoppingToken);

            if (!pendingBookingIds.Any())
                return;

            _logger.LogInformation($"Найдено {pendingBookingIds.Count} броней в статусе Pending");

            // ✅ Каждая задача получает свой scope
            var tasks = pendingBookingIds.Select(bookingId =>
                ProcessBookingWithOwnScopeAsync(bookingId, stoppingToken));

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingWithOwnScopeAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            // ✅ Создаём отдельный scope для каждой брони
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BookingBackgroundService>>();

            try
            {
                logger.LogInformation($"Обработка брони #{bookingId}...");
                await Task.Delay(2000, stoppingToken);

                // Получаем бронь вместе со связанным событием
                var booking = await dbContext.Bookings
                    .Include(b => b.Event)
                    .FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

                if (booking == null)
                {
                    logger.LogWarning($"Бронь #{bookingId} не найдена");
                    return;
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    logger.LogInformation($"Бронь #{bookingId} уже обработана (статус: {booking.Status})");
                    return;
                }

                // Проверяем, существует ли событие
                if (booking.Event == null)
                {
                    booking.Reject();
                    await dbContext.SaveChangesAsync(stoppingToken);
                    logger.LogWarning($"Бронь #{bookingId} отклонена: событие не найдено");
                    return;
                }

                // Подтверждаем бронь
                booking.Confirm();
                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation($"Бронь #{bookingId} подтверждена");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning($"Обработка брони #{bookingId} отменена");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Ошибка при обработке брони #{bookingId}");

                // Пытаемся отклонить бронь в случае ошибки
                try
                {
                    var booking = await dbContext.Bookings
                        .Include(b => b.Event)
                        .FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

                    if (booking != null && booking.Status == BookingStatus.Pending)
                    {
                        if (booking.Event != null)
                        {
                            booking.Event.ReleaseSeats();
                        }
                        booking.Reject();
                        await dbContext.SaveChangesAsync(stoppingToken);
                        logger.LogWarning($"Бронь #{bookingId} отклонена из-за ошибки");
                    }
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, $"Критическая ошибка при отклонении брони #{bookingId}");
                }
            }
        }
    }
}