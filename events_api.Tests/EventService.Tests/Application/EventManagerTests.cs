using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Application.Services;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventService.Tests.Application;

public class EventManagerTests
{
    private readonly Mock<IEventRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly EventManager _manager;

    public EventManagerTests()
    {
        _repoMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<ICacheService>();

        _manager = new EventManager(
            _repoMock.Object,
            _cacheMock.Object,
            NullLogger<EventManager>.Instance);
    }

    // Создание события

    [Fact]
    public async Task CreateEventAsync_WithValidData_ShouldCreateEvent()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var result = await _manager.CreateEventAsync(
            "Конференция",
            "Описание",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            100);

        result.Should().NotBeNull();
        result.Title.Should().Be("Конференция");
        result.TotalSeats.Should().Be(100);
        result.AvailableSeats.Should().Be(100);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_WithZeroSeats_ShouldThrowValidationException()
    {
        var act = () => _manager.CreateEventAsync(
            "Тест", null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            0);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateEventAsync_WithEndBeforeStart_ShouldThrowValidationException()
    {
        var act = () => _manager.CreateEventAsync(
            "Тест", null,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1),
            10);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // Получение по id — Cache-Aside

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_ShouldNotCallRepository()
    {
        var eventId = Guid.NewGuid();
        var cachedResponse = new EventResponse
        {
            Id = eventId,
            Title = "Из кеша",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10,
            AvailableSeats = 5
        };

        _cacheMock
            .Setup(c => c.GetAsync<EventResponse>($"event:{eventId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var result = await _manager.GetByIdAsync(eventId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Из кеша");

        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldLoadFromRepositoryAndCache()
    {
        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            "Из БД",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        _cacheMock
            .Setup(c => c.GetAsync<EventResponse>($"event:{eventId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventResponse?)null);

        _repoMock
            .Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        var result = await _manager.GetByIdAsync(eventId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Из БД");

        _repoMock.Verify(r => r.GetByIdAsync(eventId), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync(
                $"event:{eventId}",
                It.IsAny<EventResponse>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var eventId = Guid.NewGuid();

        _cacheMock
            .Setup(c => c.GetAsync<EventResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventResponse?)null);

        _repoMock
            .Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        var result = await _manager.GetByIdAsync(eventId);

        result.Should().BeNull();
        _cacheMock.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<EventResponse>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Обновление

    [Fact]
    public async Task UpdateAsync_WithExistingId_ShouldUpdateEvent()
    {
        var eventId = Guid.NewGuid();
        var existing = new Event(
            "Старое",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        _repoMock
            .Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        var result = await _manager.UpdateAsync(
            eventId,
            "Новое",
            "Описание",
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(4),
            20);

        result.Title.Should().Be("Новое");
        result.TotalSeats.Should().Be(20);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

        var act = () => _manager.UpdateAsync(
            Guid.NewGuid(),
            "Новое",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Удаление

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldDelete()
    {
        var eventId = Guid.NewGuid();

        _repoMock.Setup(r => r.ExistsAsync(eventId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.DeleteAsync(eventId)).Returns(Task.CompletedTask);

        await _manager.DeleteAsync(eventId);

        _repoMock.Verify(r => r.DeleteAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repoMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var act = () => _manager.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Топ-10 — Cache-Aside

    [Fact]
    public async Task GetTopEventsAsync_WhenCacheHit_ShouldNotCallRepository()
    {
        var cached = new List<EventResponse>
        {
            new() { Id = Guid.NewGuid(), Title = "Из кеша", TotalSeats = 10, AvailableSeats = 2 }
        };

        _cacheMock
            .Setup(c => c.GetAsync<List<EventResponse>>("events:top10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _manager.GetTopEventsAsync(10);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Из кеша");

        _repoMock.Verify(
            r => r.GetTopBySalesPercentageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopEventsAsync_WhenCacheMiss_ShouldLoadFromRepositoryAndCache()
    {
        var events = new List<Event>
        {
            new("Популярное", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        };

        _cacheMock
            .Setup(c => c.GetAsync<List<EventResponse>>("events:top10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EventResponse>?)null);

        _repoMock
            .Setup(r => r.GetTopBySalesPercentageAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var result = await _manager.GetTopEventsAsync(10);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Популярное");

        _repoMock.Verify(
            r => r.GetTopBySalesPercentageAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            c => c.SetAsync(
                "events:top10",
                It.IsAny<List<EventResponse>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}