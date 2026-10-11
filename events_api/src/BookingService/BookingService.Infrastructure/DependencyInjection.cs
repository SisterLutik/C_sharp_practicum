using BookingService.Application.Interfaces;
using BookingService.Infrastructure.DataAccess;
using BookingService.Infrastructure.HttpClients;
using BookingService.Infrastructure.Kafka;
using BookingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddHttpClient<IEventServiceClient, EventServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:EventService:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }
}