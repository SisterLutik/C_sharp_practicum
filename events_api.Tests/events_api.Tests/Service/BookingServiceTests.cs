using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using EventsApi.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventsApi.Tests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly string _dbName;
    private readonly IServiceProvider _serviceProvider;

    public BookingServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddSingleton<ILogger<EventService>>(NullLogger<EventService>.Instance);
        services.AddSingleton<ILogger<BookingService>>(NullLogger<BookingService>.Instance);

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task<(Event Event, User User)> SetupAsync(int totalSeats = 10)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User("testuser", "hashedpassword");
        var eventItem = new Event(
            "Тестовое событие",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats);

        await context.Users.AddAsync(user);
        await context.Events.AddAsync(eventItem);
        await context.SaveChangesAsync();

        return (eventItem, user);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeatsByOne()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync(10);

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Assert
        booking.Should().NotBeNull();
        booking.UserId.Should().Be(user.Id);
        var updatedEvent = await context.Events.FindAsync(eventItem.Id);
        updatedEvent!.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldSetStatusPending()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Assert
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistingEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid(), user.Id));

        exception.Message.Should().Contain("не найдено");
    }

    [Fact]
    public async Task CreateBookingAsync_WithNoAvailableSeats_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync(1);

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Занимаем единственное место
        await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(eventItem.Id, user.Id));

        exception.Message.Should().Be("No available seats for this event");
    }

    [Fact]
    public async Task CreateBookingAsync_ForStartedEvent_ShouldThrowEventAlreadyStartedException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user = new User("starteduser", "hash");
        var startedEvent = new Event(
            "Уже началось",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1),
            10);

        await context.Users.AddAsync(user);
        await context.Events.AddAsync(startedEvent);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(startedEvent.Id, user.Id));

        exception.Message.Should().Contain("уже началось");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitExceeded_ShouldThrowBookingLimitExceededException()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Создаём 3 разных события
        for (int i = 0; i < 3; i++)
        {
            var ev = new Event(
                $"Событие {i}",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();

            await bookingService.CreateBookingAsync(ev.Id, user.Id);
        }

        // 4-е событие — должно упасть
        var extraEvent = new Event(
            "Лишнее событие",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);
        await context.Events.AddAsync(extraEvent);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(extraEvent.Id, user.Id));

        exception.Message.Should().Contain("лимит");
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ShouldReturnBooking()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Act
        var result = await bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.UserId.Should().Be(user.Id);
        result.EventId.Should().Be(eventItem.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            bookingService.GetBookingByIdAsync(Guid.NewGuid()));

        exception.Message.Should().Contain("не найдена");
    }

    [Fact]
    public async Task CancelBookingAsync_ByOwner_ShouldSetStatusCancelled()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Act
        await bookingService.CancelBookingAsync(booking.Id, user.Id, UserRole.User);

        // Assert
        var updatedBooking = await context.Bookings.FindAsync(booking.Id);
        updatedBooking!.Status.Should().Be(BookingStatus.Cancelled);
        updatedBooking.ProcessedAt.Should().NotBeNull();

        // Место вернулось в пул
        var updatedEvent = await context.Events.FindAsync(eventItem.Id);
        updatedEvent!.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public async Task CancelBookingAsync_ByOtherUser_ShouldThrowForbiddenOperationException()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);
        var otherUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            bookingService.CancelBookingAsync(booking.Id, otherUserId, UserRole.User));

        exception.Message.Should().Contain("Нет прав");
    }

    [Fact]
    public async Task CancelBookingAsync_ByAdmin_ShouldSucceed()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);
        var adminId = Guid.NewGuid();

        // Act
        await bookingService.CancelBookingAsync(booking.Id, adminId, UserRole.Admin);

        // Assert
        var updatedBooking = await context.Bookings.FindAsync(booking.Id);
        updatedBooking!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_Twice_ShouldThrowBookingAlreadyCancelledException()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);
        await bookingService.CancelBookingAsync(booking.Id, user.Id, UserRole.User);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() =>
            bookingService.CancelBookingAsync(booking.Id, user.Id, UserRole.User));

        exception.Message.Should().Contain("уже отменена");
    }

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        context.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}