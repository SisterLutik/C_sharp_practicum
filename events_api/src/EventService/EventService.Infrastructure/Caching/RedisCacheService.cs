using System.Text.Json;
using EventService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(_settings.InstanceName + key);

        if (value.IsNullOrEmpty)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Не удалось десериализовать значение из Redis по ключу {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        var ttl = expiry ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);

        await db.StringSetAsync(_settings.InstanceName + key, json, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(_settings.InstanceName + key);
    }
}