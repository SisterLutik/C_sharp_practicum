using EventService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EventService.Tests.Domain;

public class EventTests
{
    [Fact]
    public void Constructor_ShouldCreateEventWithAvailableSeatsEqualToTotal()
    {
        // Act
        var ev = new Event("Конференция", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        // Assert
        ev.Id.Should().NotBe(Guid.Empty);
        ev.Title.Should().Be("Конференция");
        ev.TotalSeats.Should().Be(10);
        ev.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ShouldThrow()
    {
        var act = () => new Event("", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEndBeforeStart_ShouldThrow()
    {
        var act = () => new Event("Тест", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1), 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryReserveSeats_WithEnoughSeats_ShouldReturnTrueAndDecrease()
    {
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        var result = ev.TryReserveSeats();

        result.Should().BeTrue();
        ev.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public void TryReserveSeats_WhenNoSeats_ShouldReturnFalse()
    {
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        ev.TryReserveSeats();

        var result = ev.TryReserveSeats();

        result.Should().BeFalse();
        ev.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void ReleaseSeats_ShouldIncreaseAvailableSeats()
    {
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        ev.TryReserveSeats();

        ev.ReleaseSeats();

        ev.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void ReleaseSeats_ShouldNotExceedTotalSeats()
    {
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        ev.ReleaseSeats(5);

        ev.AvailableSeats.Should().Be(10);  // не 15
    }

    [Fact]
    public void HasStarted_WithPastStartAt_ShouldReturnTrue()
    {
        var ev = new Event("Прошлое", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), 10);

        ev.HasStarted().Should().BeTrue();
    }

    [Fact]
    public void HasStarted_WithFutureStartAt_ShouldReturnFalse()
    {
        var ev = new Event("Будущее", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);

        ev.HasStarted().Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_WithIncreasedTotalSeats_ShouldAddSeatsToAvailable()
    {
        var ev = new Event("Тест", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        ev.TryReserveSeats(3);  // AvailableSeats = 7

        ev.UpdateDetails("Обновлено", null, DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20);

        ev.TotalSeats.Should().Be(20);
        ev.AvailableSeats.Should().Be(17);  // 7 + 10 новых
    }
}