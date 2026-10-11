using System.Text.Json;

namespace EventsApi.Contracts.Kafka;

/// <summary>
/// Единые настройки JSON-сериализации для Kafka.
/// </summary>
public static class KafkaJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}