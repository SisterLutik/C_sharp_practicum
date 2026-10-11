using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventsApi.Contracts.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Infrastructure.Kafka;

/// <summary>
/// Hosted-сервис, который при старте приложения создаёт Kafka-топик,
/// если его ещё нет. Иначе подписчик может не найти топик при первом запуске.
/// Ошибки создания не валят приложение — только логируются.
/// </summary>
public class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaTopicInitializer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _settings.BootstrapServers
            }).Build();

            await adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingConfirmed,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });

            _logger.LogInformation(
                "Топик {Topic} успешно создан", KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex)
            when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation(
                "Топик {Topic} уже существует", KafkaTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            //  Не валим приложение, если Kafka недоступна на старте
            _logger.LogWarning(ex,
                "Не удалось создать топик {Topic} при старте. " +
                "Возможно, Kafka ещё не готова. Подписчик попробует позже.",
                KafkaTopics.BookingConfirmed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}