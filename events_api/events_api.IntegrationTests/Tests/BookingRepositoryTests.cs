using events_api.IntegrationTests.Fixtures;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Infrastructure;
using EventsApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventsApi.IntegrationTests.Tests;

public class BookingRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public BookingRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Event Event, User User, BookingRepository BookingRepo, EventRepository EventRepo)> SetupAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var eventRepo = new EventRepository(_fixture.DbContext);
        var bookingRepo = new BookingRepository(_fixture.DbContext);

        var user = new User("testuser", "hashedpassword");
        await _fixture.DbContext.Users.AddAsync(user);

        var eventItem = new Event(
            "Тестовое событие",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10
        );
        await eventRepo.AddAsync(eventItem);

        return (eventItem, user, bookingRepo, eventRepo);
    }

    [Fact]
    public async Task AddAsync_ShouldAddBookingToDatabase()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        // Act
        var booking = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking);

        // Assert
        var saved = await _fixture.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        saved.Should().NotBeNull();
        saved!.EventId.Should().Be(eventItem.Id);
        saved.UserId.Should().Be(user.Id);
        saved.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnBooking()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking);

        // Act
        var result = await bookingRepo.GetByIdAsync(booking.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(eventItem.Id);
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var bookingRepo = new BookingRepository(_fixture.DbContext);

        // Act
        var result = await bookingRepo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnBookingsForEvent()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var booking1 = new Booking(eventItem.Id, user.Id);
        var booking2 = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking1);
        await bookingRepo.AddAsync(booking2);

        // Act
        var bookings = await bookingRepo.GetByEventIdAsync(eventItem.Id);

        // Assert
        bookings.Should().HaveCount(2);
        bookings.Should().AllSatisfy(b => b.EventId.Should().Be(eventItem.Id));
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingBookings()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var pendingBooking = new Booking(eventItem.Id, user.Id);
        var confirmedBooking = new Booking(eventItem.Id, user.Id);
        confirmedBooking.Confirm();

        await bookingRepo.AddAsync(pendingBooking);
        await bookingRepo.AddAsync(confirmedBooking);

        // Act
        var pending = await bookingRepo.GetPendingAsync();

        // Assert
        pending.Should().HaveCount(1);
        pending.First().Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetActiveByUserAsync_ShouldReturnPendingAndConfirmedBookings()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var pending = new Booking(eventItem.Id, user.Id);
        var confirmed = new Booking(eventItem.Id, user.Id);
        confirmed.Confirm();
        var cancelled = new Booking(eventItem.Id, user.Id);
        cancelled.Cancel();

        await bookingRepo.AddAsync(pending);
        await bookingRepo.AddAsync(confirmed);
        await bookingRepo.AddAsync(cancelled);

        // Act
        var active = await bookingRepo.GetActiveByUserAsync(user.Id);

        // Assert
        active.Should().HaveCount(2);
        active.Should().Contain(b => b.Status == BookingStatus.Pending);
        active.Should().Contain(b => b.Status == BookingStatus.Confirmed);
        active.Should().NotContain(b => b.Status == BookingStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBooking()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking);

        // Act
        booking.Confirm();
        await bookingRepo.UpdateAsync(booking);

        // Assert
        var updated = await bookingRepo.GetByIdAsync(booking.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(BookingStatus.Confirmed);
        updated.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteBooking()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking);

        // Act
        await bookingRepo.DeleteAsync(booking.Id);

        // Assert
        var result = await bookingRepo.GetByIdAsync(booking.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForExistingBooking()
    {
        // Arrange
        var (eventItem, user, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id, user.Id);
        await bookingRepo.AddAsync(booking);

        // Act
        var exists = await bookingRepo.ExistsAsync(booking.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForNonExistingBooking()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var bookingRepo = new BookingRepository(_fixture.DbContext);

        // Act
        var exists = await bookingRepo.ExistsAsync(Guid.NewGuid());

        // Assert
        exists.Should().BeFalse();
    }
}