using events_api.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace events_api.IntegrationTests.Fixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    public AppDbContext DbContext { get; private set; } = null!;

    public TestDatabaseFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("events_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        DbContext = new AppDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        // Удаляем и пересоздаём схему
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.MigrateAsync();
    }
}