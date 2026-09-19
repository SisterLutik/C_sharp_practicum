using EventsApi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventsApi.Presentation.BackgroundServices;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var pendingBookings = await bookingRepository.GetPendingAsync();

            var tasks = pendingBookings.Select(b =>
                ProcessWithScopeAsync(b.Id, stoppingToken));

            await Task.WhenAll(tasks);
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessWithScopeAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();
        await processor.ProcessBookingAsync(bookingId, stoppingToken);
    }
}