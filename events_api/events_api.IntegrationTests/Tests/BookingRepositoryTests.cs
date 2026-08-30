using events_api.Data.Repositories;
using events_api.Models;
using events_api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;  // ← Добавьте
using Xunit;

namespace events_api.IntegrationTests.Tests;

public class BookingRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public BookingRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Event Event, BookingRepository BookingRepo, EventRepository EventRepo)> SetupAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var eventRepo = new EventRepository(_fixture.DbContext);
        var bookingRepo = new BookingRepository(_fixture.DbContext);

        var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);
        await eventRepo.AddAsync(eventItem);

        return (eventItem, bookingRepo, eventRepo);
    }

    [Fact]
    public async Task AddAsync_ShouldAddBookingToDatabase()
    {
        // Arrange
        var (eventItem, bookingRepo, _) = await SetupAsync();

        // Act
        var booking = new Booking(eventItem.Id);
        await bookingRepo.AddAsync(booking);

        // Assert
        var saved = await _fixture.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id);  // ✅ работает

        saved.Should().NotBeNull();
        saved!.EventId.Should().Be(eventItem.Id);
        saved.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnBooking()
    {
        // Arrange
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id);
        await bookingRepo.AddAsync(booking);

        // Act
        var result = await bookingRepo.GetByIdAsync(booking.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(eventItem.Id);
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
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var booking1 = new Booking(eventItem.Id);
        var booking2 = new Booking(eventItem.Id);
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
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var pendingBooking = new Booking(eventItem.Id);
        var confirmedBooking = new Booking(eventItem.Id);
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
    public async Task UpdateAsync_ShouldUpdateBooking()
    {
        // Arrange
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id);
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
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id);
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
        var (eventItem, bookingRepo, _) = await SetupAsync();

        var booking = new Booking(eventItem.Id);
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