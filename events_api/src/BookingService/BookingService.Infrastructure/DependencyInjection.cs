using BookingService.Application.Interfaces;
using BookingService.Infrastructure.DataAccess;
using BookingService.Infrastructure.HttpClients;
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
        // DbContext
        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Репозитории
        services.AddScoped<IBookingRepository, BookingRepository>();

        // HTTP-клиент к EventService (заготовка на будущее)
        services.AddHttpClient<IEventServiceClient, EventServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:EventService:BaseUrl"]!);
        });

        return services;
    }
}