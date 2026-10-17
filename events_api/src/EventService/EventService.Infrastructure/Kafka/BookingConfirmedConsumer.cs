using System.Text.Json;
using Confluent.Kafka;
using EventService.Infrastructure.DataAccess;
using EventsApi.Contracts.Kafka;
using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Infrastructure.Kafka;

/// <summary>
/// Фоновый подписчик на топик booking-confirmed.
/// При получении сообщения уменьшает AvailableSeats у события.
/// </summary>
public class BookingConfirmedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingConfirmedConsumer> _logger;
    private readonly KafkaSettings _settings;

    public BookingConfirmedConsumer(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> settings,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmedConsumer запущен");

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingConfirmed);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Consume — блокирующий метод, но уважает CancellationToken.
                    // Возвращает null по таймауту.
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value == null)
                        continue;

                    await HandleMessageAsync(result.Message.Value, stoppingToken);

                    // Offset коммитим только после успешной обработки.
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка чтения из Kafka");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // НЕ валим сервис на одном «плохом» сообщении.
                    _logger.LogError(ex, "Ошибка обработки сообщения — пропускаем");
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("BookingConfirmedConsumer остановлен");
        }
    }

    private async Task HandleMessageAsync(string json, CancellationToken stoppingToken)
    {
        BookingConfirmed? booking;
        try
        {
            booking = JsonSerializer.Deserialize<BookingConfirmed>(json, KafkaJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Не удалось десериализовать сообщение: {Json}", json);
            return;
        }

        if (booking == null) return;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var alreadyProcessed = await context.ProcessedBookings
            .AnyAsync(p => p.BookingId == booking.BookingId, stoppingToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Бронь {BookingId} уже обработана — пропускаем", booking.BookingId);
            return;
        }

        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);

        if (eventEntity == null)
        {
            _logger.LogWarning(
                "Событие {EventId} не найдено. Бронь {BookingId} — пропускаем",
                booking.EventId, booking.BookingId);
            return;
        }

        // Проверка: событие ещё не началось
        if (eventEntity.HasStarted())
        {
            _logger.LogWarning(
                "Событие {EventId} уже началось. Бронь {BookingId} — пропускаем",
                booking.EventId, booking.BookingId);
            return;
        }

        if (!eventEntity.TryReserveSeats(booking.SeatsCount))
        {
            _logger.LogWarning(
                "Нет доступных мест у события {EventId}. Бронь {BookingId} — пропускаем",
                booking.EventId, booking.BookingId);
            return;
        }

        // Записываем в обработанные
        context.ProcessedBookings.Add(new ProcessedBooking(booking.BookingId));

        await context.SaveChangesAsync(stoppingToken);

        _logger.LogInformation(
            "Уменьшено на {Seats} мест событие {EventId}. Осталось: {Available}",
            booking.SeatsCount, booking.EventId, eventEntity.AvailableSeats);
    }
}