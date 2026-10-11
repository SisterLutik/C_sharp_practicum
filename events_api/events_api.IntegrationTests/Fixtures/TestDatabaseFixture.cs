using BookingService.Infrastructure.DataAccess;
using EventService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using UserService.Infrastructure.DataAccess;
using Xunit;

namespace IntegrationTests.Fixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public UserDbContext UserContext { get; private set; } = null!;
    public EventDbContext EventContext { get; private set; } = null!;
    public BookingDbContext BookingContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var connStr = _postgres.GetConnectionString();

        UserContext = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connStr).Options);

        EventContext = new EventDbContext(
            new DbContextOptionsBuilder<EventDbContext>().UseNpgsql(connStr).Options);

        BookingContext = new BookingDbContext(
            new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(connStr).Options);
    }

    public async Task DisposeAsync()
    {
        await UserContext.DisposeAsync();
        await EventContext.DisposeAsync();
        await BookingContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        // Удаляем всю схему и накатываем миграции заново
        await UserContext.Database.EnsureDeletedAsync();
        await UserContext.Database.MigrateAsync();
        await EventContext.Database.MigrateAsync();
        await BookingContext.Database.MigrateAsync();
    }
}