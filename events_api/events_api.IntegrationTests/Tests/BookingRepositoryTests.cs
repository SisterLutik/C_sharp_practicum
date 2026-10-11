using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Repositories;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests;

public class BookingRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public BookingRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveBooking()
    {
        await _fixture.ResetAsync();
        var repo = new BookingRepository(_fixture.BookingContext);

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await repo.AddAsync(booking);

        var saved = await repo.GetByIdAsync(booking.Id);

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetActiveByUserAsync_ShouldReturnPendingAndConfirmed()
    {
        await _fixture.ResetAsync();
        var repo = new BookingRepository(_fixture.BookingContext);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var pending = new Booking(eventId, userId);
        var confirmed = new Booking(eventId, userId);
        confirmed.Confirm();
        var cancelled = new Booking(eventId, userId);
        cancelled.Cancel();

        await repo.AddAsync(pending);
        await repo.AddAsync(confirmed);
        await repo.AddAsync(cancelled);

        var active = await repo.GetActiveByUserAsync(userId);

        active.Should().HaveCount(2);
        active.Should().Contain(b => b.Status == BookingStatus.Pending);
        active.Should().Contain(b => b.Status == BookingStatus.Confirmed);
        active.Should().NotContain(b => b.Status == BookingStatus.Cancelled);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPending()
    {
        await _fixture.ResetAsync();
        var repo = new BookingRepository(_fixture.BookingContext);

        await repo.AddAsync(new Booking(Guid.NewGuid(), Guid.NewGuid()));
        var confirmed = new Booking(Guid.NewGuid(), Guid.NewGuid());
        confirmed.Confirm();
        await repo.AddAsync(confirmed);

        var pending = await repo.GetPendingAsync();

        pending.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateStatus()
    {
        await _fixture.ResetAsync();
        var repo = new BookingRepository(_fixture.BookingContext);

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await repo.AddAsync(booking);

        booking.Confirm();
        await repo.UpdateAsync(booking);

        var updated = await repo.GetByIdAsync(booking.Id);
        updated!.Status.Should().Be(BookingStatus.Confirmed);
        updated.ProcessedAt.Should().NotBeNull();
    }
}