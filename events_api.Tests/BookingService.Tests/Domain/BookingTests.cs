using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BookingService.Tests.Domain;

public class BookingTests
{
    //  Создание

    [Fact]
    public void Constructor_WithValidData_ShouldCreatePendingBooking()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);

        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyEventId_ShouldThrow()
    {
        var act = () => new Booking(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>().WithMessage("*EventId*");
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrow()
    {
        var act = () => new Booking(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    //  Confirm

    [Fact]
    public void Confirm_FromPending_ShouldSetConfirmed()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Confirm_FromConfirmed_ShouldThrow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();

        var act = () => booking.Confirm();

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void Confirm_FromCancelled_ShouldThrow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();

        var act = () => booking.Confirm();

        act.Should().Throw<ForbiddenOperationException>();
    }

    //  Reject

    [Fact]
    public void Reject_FromPending_ShouldSetRejected()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

        booking.Reject();

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_FromConfirmed_ShouldThrow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();

        var act = () => booking.Reject();

        act.Should().Throw<ForbiddenOperationException>();
    }

    //  Cancel

    [Fact]
    public void Cancel_FromPending_ShouldSetCancelled()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldSucceed()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Twice_ShouldThrowBookingAlreadyCancelledException()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();

        var act = () => booking.Cancel();

        act.Should().Throw<BookingAlreadyCancelledException>();
    }

    [Fact]
    public void Cancel_FromRejected_ShouldThrow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.Reject();

        var act = () => booking.Cancel();

        act.Should().Throw<ForbiddenOperationException>();
    }
}