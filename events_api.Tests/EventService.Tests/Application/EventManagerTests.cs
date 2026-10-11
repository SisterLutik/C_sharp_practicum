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
    private readonly EventManager _manager;

    public EventManagerTests()
    {
        _repoMock = new Mock<IEventRepository>();
        _manager = new EventManager(_repoMock.Object, NullLogger<EventManager>.Instance);
    }

    // ============================================
    // ✅ Создание события
    // ============================================

    [Fact]
    public async Task CreateEventAsync_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);

        // Act
        var result = await _manager.CreateEventAsync(
            "Конференция",
            "Описание",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            100);

        // Assert
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
            "Тест", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 0);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*TotalSeats*");
    }

    [Fact]
    public async Task CreateEventAsync_WithEndBeforeStart_ShouldThrowValidationException()
    {
        var act = () => _manager.CreateEventAsync(
            "Тест", null, DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1), 10);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*EndAt*");
    }

    // ============================================
    // ✅ Обновление
    // ============================================

    [Fact]
    public async Task UpdateAsync_WithExistingId_ShouldUpdateEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existing = new Event("Старое", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        _repoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);

        // Act
        var result = await _manager.UpdateAsync(
            eventId, "Новое", "Описание",
            DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20);

        // Assert
        result.Title.Should().Be("Новое");
        result.TotalSeats.Should().Be(20);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        var act = () => _manager.UpdateAsync(
            Guid.NewGuid(), "Новое", null,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============================================
    // ✅ Удаление
    // ============================================

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
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var act = () => _manager.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============================================
    // ✅ TryReserveSeatsAsync (межсервисный)
    // ============================================

    [Fact]
    public async Task TryReserveSeatsAsync_WithExistingEventAndSeats_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        _repoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);

        // Act
        var result = await _manager.TryReserveSeatsAsync(eventId);

        // Assert
        result.Success.Should().BeTrue();
        result.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WithNonExistingEvent_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        var result = await _manager.TryReserveSeatsAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("Event not found");
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WithNoAvailableSeats_ShouldReturnFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        ev.TryReserveSeats();

        _repoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);

        // Act
        var result = await _manager.TryReserveSeatsAsync(eventId);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Be("No available seats for this event");
    }

    // ============================================
    // ✅ ReleaseSeatsAsync (межсервисный)
    // ============================================

    [Fact]
    public async Task ReleaseSeatsAsync_WithExistingEvent_ShouldReturnTrue()
    {
        var eventId = Guid.NewGuid();
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        ev.TryReserveSeats();

        _repoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(ev);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);

        var result = await _manager.ReleaseSeatsAsync(eventId);

        result.Should().BeTrue();
        ev.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public async Task ReleaseSeatsAsync_WithNonExistingEvent_ShouldReturnFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        var result = await _manager.ReleaseSeatsAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}