using events_api.Data.Repositories;
using events_api.Models;
using events_api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore; 
using Xunit;

namespace events_api.IntegrationTests.Tests;

public class EventRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public EventRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldAddEventToDatabase()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var eventItem = new Event(
            "Интеграционное событие",
            DateTime.Now.AddDays(1),
            DateTime.Now.AddDays(2),
            50,
            "Тестовое описание"
        );

        // Act
        await repository.AddAsync(eventItem);

        // Assert
        var saved = await _fixture.DbContext.Events
            .FirstOrDefaultAsync(e => e.Title == "Интеграционное событие");

        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Интеграционное событие");
    }
}