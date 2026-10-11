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
    private readonly Mock<IEventServiceClient> _eventClientMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingManager _manager;

    public BookingManagerTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventClientMock = new Mock<IEventServiceClient>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _manager = new BookingManager(
            _bookingRepoMock.Object,
            _eventClientMock.Object,
            _eventPublisherMock.Object,
            NullLogger<BookingManager>.Instance);
    }

    // ============================================
    // ✅ Успешные сценарии
    // ============================================

    [Fact]
    public async Task CreateBookingAsync_WithValidEventAndUser_ShouldCreateBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 10,
                AvailableSeats = 10
            });

        _eventClientMock
            .Setup(c => c.TryReserveSeatsAsync(eventId))
            .ReturnsAsync(new ReserveSeatsResponse { Success = true, AvailableSeats = 9 });

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
        _eventClientMock.Verify(c => c.TryReserveSeatsAsync(eventId), Times.Once);
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
        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 10,
                AvailableSeats = 9
            });

        // Act
        await _manager.CancelBookingAsync(bookingId, userId, isAdmin: false);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
        _bookingRepoMock.Verify(r => r.UpdateAsync(booking), Times.Once);
        _eventClientMock.Verify(c => c.ReleaseSeatsAsync(eventId), Times.Once);
    }

    // ============================================
    // ❌ Неуспешные сценарии
    // ============================================

    [Fact]
    public async Task CreateBookingAsync_WithNonExistingEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync((EventDto?)null);

        // Act
        var act = () => _manager.CreateBookingAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateBookingAsync_ForStartedEvent_ShouldThrowEventAlreadyStartedException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(-2),  // в прошлом
                EndAt = DateTime.UtcNow.AddDays(-1),
                TotalSeats = 10,
                AvailableSeats = 10
            });

        // Act
        var act = () => _manager.CreateBookingAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<EventAlreadyStartedException>();
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitExceeded_ShouldThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 10,
                AvailableSeats = 10
            });

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
    public async Task CreateBookingAsync_WhenNoSeats_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 10,
                AvailableSeats = 0
            });

        _bookingRepoMock
            .Setup(r => r.GetActiveByUserAsync(userId))
            .ReturnsAsync(new List<Booking>());

        _eventClientMock
            .Setup(c => c.TryReserveSeatsAsync(eventId))
            .ReturnsAsync(new ReserveSeatsResponse
            {
                Success = false,
                Reason = "No available seats for this event"
            });

        // Act
        var act = () => _manager.CreateBookingAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task CancelBookingAsync_ByOtherUser_ShouldThrowForbiddenOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var booking = new Booking(eventId, ownerId);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);

        // Act
        var act = () => _manager.CancelBookingAsync(bookingId, otherUserId, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOperationException>();
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
        _eventClientMock
            .Setup(c => c.GetEventAsync(eventId))
            .ReturnsAsync(new EventDto { Id = eventId, StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = 10, AvailableSeats = 9 });

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