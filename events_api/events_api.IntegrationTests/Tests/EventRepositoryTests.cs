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
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
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

    [Fact]
    public async Task GetAllAsync_WithDateFilters_ShouldReturnEventsInRange()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var baseDate = DateTime.UtcNow.Date;

        await repository.AddAsync(new Event(
            "Событие в диапазоне",
            baseDate.AddDays(-5),
            baseDate.AddDays(5),
            10
        ));
        await repository.AddAsync(new Event(
            "Событие вне диапазона",
            baseDate.AddDays(-20),
            baseDate.AddDays(-15),
            10
        ));

        var from = baseDate.AddDays(-10);
        var to = baseDate.AddDays(10);

        // Act
        var result = await repository.GetAllAsync(null, from, to, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("Событие в диапазоне");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        for (int i = 0; i < 25; i++)
        {
            await repository.AddAsync(new Event(
                $"Событие {i}",
                DateTime.UtcNow.AddDays(i),
                DateTime.UtcNow.AddDays(i + 1),
                10
            ));
        }

        // Act
        var result = await repository.GetAllAsync(null, null, null, 2, 10);

        // Assert
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(25);
    }
}