using EventsApi.Application.Interfaces;
using EventsApi.Application.Settings;
using EventsApi.Infrastructure.Repositories;
using EventsApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace EventsApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // База данных
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // JWT-настройки
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Компоненты безопасности
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}