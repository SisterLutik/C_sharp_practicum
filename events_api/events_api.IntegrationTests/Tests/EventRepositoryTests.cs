using events_api.IntegrationTests.Fixtures;
using EventsApi.Domain.Entities;
using EventsApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventsApi.IntegrationTests.Tests;

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
        saved.Description.Should().Be("Тестовое описание");
        saved.TotalSeats.Should().Be(50);
        saved.AvailableSeats.Should().Be(50);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEvent()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var eventItem = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repository.AddAsync(eventItem);

        // Act
        var result = await repository.GetByIdAsync(eventItem.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventItem.Id);
        result.Title.Should().Be("Тест");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        await repository.AddAsync(new Event("Уникальное название", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10));
        await repository.AddAsync(new Event("Другое событие", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 10));

        // Act
        var result = await repository.GetAllAsync("уникальное", null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("Уникальное название");
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

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEvent()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var eventItem = new Event("Старое название", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repository.AddAsync(eventItem);

        eventItem.UpdateDetails("Новое название", "Новое описание", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20);

        // Act
        await repository.UpdateAsync(eventItem);

        // Assert
        var updated = await repository.GetByIdAsync(eventItem.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Новое название");
        updated.Description.Should().Be("Новое описание");
        updated.TotalSeats.Should().Be(20);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEvent()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var eventItem = new Event("Событие для удаления", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repository.AddAsync(eventItem);

        // Act
        await repository.DeleteAsync(eventItem.Id);

        // Assert
        var result = await repository.GetByIdAsync(eventItem.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForExistingEvent()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        var eventItem = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await repository.AddAsync(eventItem);

        // Act
        var exists = await repository.ExistsAsync(eventItem.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForNonExistingEvent()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var repository = new EventRepository(_fixture.DbContext);

        // Act
        var exists = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        exists.Should().BeFalse();
    }
}