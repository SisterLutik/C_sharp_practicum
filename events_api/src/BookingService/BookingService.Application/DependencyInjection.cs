using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingManager>();
        services.AddScoped<IBookingProcessor, BookingProcessor>();

        return services;
    }
}