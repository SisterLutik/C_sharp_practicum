using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using EventsApi.Contracts.Events;
using EventsApi.Contracts.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BookingService.Tests.Application;

public class BookingManagerTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingManager _manager;

    public BookingManagerTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();

       _manager = new BookingManager(
       _bookingRepoMock.Object,
       _eventPublisherMock.Object,
       NullLogger<BookingManager>.Instance);
    }

    // ✅ Успешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WithValidEventAndUser_ShouldCreateBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookingRepoMock
            .Setup(r => r.GetActiveByUserAsync(userId))
            .ReturnsAsync(new List<Booking>());

        Booking? capturedBooking = null;
        _bookingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Callback<Booking>(b => capturedBooking = b)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _manager.CreateBookingAsync(eventId, userId);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(eventId);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(BookingStatus.Pending);

        _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ByOwner_ShouldCancelAndReleaseSeat()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        
        // Act
        await _manager.CancelBookingAsync(bookingId, userId, isAdmin: false);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
        _bookingRepoMock.Verify(r => r.UpdateAsync(booking), Times.Once);
    }

    // ❌ Неуспешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitExceeded_ShouldThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        
        // 10 активных броней — лимит достигнут
        var activeBookings = Enumerable.Range(0, 10)
            .Select(_ => new Booking(eventId, userId))
            .ToList();

        _bookingRepoMock
            .Setup(r => r.GetActiveByUserAsync(userId))
            .ReturnsAsync(activeBookings);

        // Act
        var act = () => _manager.CreateBookingAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<BookingLimitExceededException>();
    }

    [Fact]
    public async Task CancelBookingAsync_ByAdmin_ShouldSucceed()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var booking = new Booking(eventId, ownerId);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);
       
        // Act
        await _manager.CancelBookingAsync(bookingId, adminId, isAdmin: true);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_Twice_ShouldThrowBookingAlreadyCancelledException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = new Booking(eventId, userId);
        booking.Cancel();

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);

        // Act
        var act = () => _manager.CancelBookingAsync(bookingId, userId, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<BookingAlreadyCancelledException>();
    }
}