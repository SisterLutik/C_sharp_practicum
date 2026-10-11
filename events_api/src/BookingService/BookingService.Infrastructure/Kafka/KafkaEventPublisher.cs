using System.Text.Json;
using BookingService.Application.Interfaces;
using Confluent.Kafka;
using EventsApi.Contracts.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Infrastructure.Kafka;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishBookingConfirmedAsync(
        BookingConfirmed @event,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(@event, KafkaJsonOptions.Default);

        var message = new Message<string, string>
        {
            // ✅ Ключ — EventId. Все брони одного события идут в один раздел
            // и обрабатываются последовательно.
            Key = @event.EventId.ToString(),
            Value = json
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(
                KafkaTopics.BookingConfirmed,
                message,
                cancellationToken);

            _logger.LogInformation(
                "Событие {EventType} отправлено в {Topic} [partition={Partition}, offset={Offset}]",
                nameof(BookingConfirmed),
                KafkaTopics.BookingConfirmed,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Ошибка отправки события {EventType} в Kafka",
                nameof(BookingConfirmed));
            throw;
        }
    }

    public void Dispose()
    {
        // ✅ Продюсер — тяжёлый потокобезопасный объект. Освобождаем при остановке приложения.
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}