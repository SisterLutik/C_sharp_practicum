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

    // GET — при ошибке возвращаем default (null)
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
                "Redis недоступен при чтении ключа {Key}. Кэш деградирован, запрос пойдёт в БД.",
                key);
            return default;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при чтении ключа {Key}. Кэш деградирован.",
                key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Ошибка десериализации значения из Redis по ключу {Key}. Значение будет проигнорировано.",
                key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при чтении ключа {Key}.",
                key);
            return default;
        }
    }

    // SET — при ошибке НЕ пробрасываем, только логируем.
    // Запись в кеш — не критично, данные всё равно в БД.
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
            var ttl = expiry ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);

            await db.StringSetAsync(_settings.InstanceName + key, json, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis недоступен при записи ключа {Key}. Кэш не обновлён, но данные уже сохранены в БД.",
                key);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при записи ключа {Key}. Кэш не обновлён.",
                key);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Ошибка сериализации значения для ключа {Key}. Кэш не обновлён.",
                key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при записи ключа {Key}.",
                key);
        }
    }

    // REMOVE — при ошибке НЕ пробрасываем, только логируем.
    // Ключ просто останется в кеше до истечения TTL,
    // следующий читающий запрос вернёт актуальные данные,
    // если TTL истечёт. Если критично — можно явно проверить
    // и попробовать инвалидировать позже.

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
                "Redis недоступен при удалении ключа {Key}. Кэш не инвалидирован, " +
                "но данные уже обновлены в БД. Ключ истечёт по TTL.",
                key);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Таймаут Redis при удалении ключа {Key}. Кэш не инвалидирован.",
                key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Неожиданная ошибка кэша при удалении ключа {Key}.",
                key);
        }
    }
}