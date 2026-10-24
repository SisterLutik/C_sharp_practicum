using System.Text.Json;
using EventService.Application.Interfaces;
using EventService.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly CacheSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<CacheSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(_settings.InstanceName + key);

            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis недоступен при чтении ключа {Key}. Кэш деградирован.", key);
            return default;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при чтении ключа {Key}. Кэш деградирован.", key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Ошибка десериализации значения из Redis по ключу {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при чтении ключа {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);

            // Если expiry не передан — используем TTL события по умолчанию.
            // Другие TTL (например, для топа) EventManager передаёт явно.
            var ttl = expiry ?? TimeSpan.FromMinutes(_settings.EventTtlMinutes);

            await db.StringSetAsync(_settings.InstanceName + key, json, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis недоступен при записи ключа {Key}. Кэш не обновлён.", key);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при записи ключа {Key}. Кэш не обновлён.", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при записи ключа {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(_settings.InstanceName + key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis недоступен при удалении ключа {Key}. Кэш не инвалидирован.", key);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при удалении ключа {Key}. Кэш не инвалидирован.", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при удалении ключа {Key}", key);
        }
    }
}