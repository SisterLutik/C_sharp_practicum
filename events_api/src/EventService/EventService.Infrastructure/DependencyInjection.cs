using EventService.Application.Interfaces;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.Kafka;
using EventService.Infrastructure.Repositories;
using EventService.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;


namespace EventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();

        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));

        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = configuration
                .GetSection(RedisSettings.SectionName)
                .Get<RedisSettings>()
                ?? throw new InvalidOperationException(
                    "Секция Redis не найдена в конфигурации");

            return ConnectionMultiplexer.Connect(settings.ConnectionString);
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;

    }
}