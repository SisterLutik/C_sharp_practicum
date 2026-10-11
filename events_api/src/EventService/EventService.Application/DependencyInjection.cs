using EventService.Application.Interfaces;
using EventService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddEventApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventManager>();

        return services;
    }
}