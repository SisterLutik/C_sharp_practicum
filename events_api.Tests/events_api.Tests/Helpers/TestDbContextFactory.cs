using events_api.DataAccess;
using events_api.Interfaces;
using events_api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace events_api.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext CreateInMemoryContext(string databaseName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
            return context;
        }

        public static ServiceProvider CreateServiceProvider(string databaseName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            return services.BuildServiceProvider();
        }
    }
}