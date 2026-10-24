using EventService.Application.Interfaces;
using EventService.Application.Settings;
using EventService.Infrastructure.Caching;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.Kafka;
using EventService.Infrastructure.Repositories;
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

        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = configuration
                .GetSection(CacheSettings.SectionName)
                .Get<CacheSettings>()
                ?? throw new InvalidOperationException(
                    "Секция Cache не найдена в конфигурации");

            var options = ConfigurationOptions.Parse(settings.ConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}