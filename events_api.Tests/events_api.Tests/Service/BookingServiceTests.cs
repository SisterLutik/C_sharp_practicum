using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using EventsApi.Infrastructure;
using EventsApi.Infrastructure.DataAccess;
using EventsApi.Infrastructure.Repositories;
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

    // =============================================
    // 🔧 Вспомогательный метод
    // =============================================

    private async Task<(Event Event, User User)> SetupAsync(
        int totalSeats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User($"user_{Guid.NewGuid():N}", "hashedpassword");

        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        var end = endAt ?? DateTime.UtcNow.AddDays(2);

        var eventItem = new Event("Тестовое событие", start, end, totalSeats);

        await context.Users.AddAsync(user);
        await context.Events.AddAsync(eventItem);
        await context.SaveChangesAsync();

        return (eventItem, user);
    }

    private async Task<User> CreateUserAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User($"user_{Guid.NewGuid():N}", "hashedpassword");
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    // =============================================
    // ✅ УСПЕШНЫЕ СЦЕНАРИИ СОЗДАНИЯ БРОНИ
    // =============================================

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
    public async Task CreateBookingAsync_MultipleBookingsUpToLimit_ShouldAllSucceed()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var eventIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 5);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            eventIds.Add(ev.Id);
        }

        // Act
        var bookings = new List<Booking>();
        foreach (var id in eventIds)
        {
            bookings.Add(await bookingService.CreateBookingAsync(id, user.Id));
        }

        // Assert
        bookings.Should().HaveCount(5);
        bookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        bookings.Should().AllSatisfy(b => b.UserId.Should().Be(user.Id));
    }

    // =============================================
    // ❌ НЕУСПЕШНЫЕ СЦЕНАРИИ СОЗДАНИЯ БРОНИ
    // =============================================

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

        await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(eventItem.Id, user.Id));

        exception.Message.Should().Be("No available seats for this event");
    }

    // =============================================
    // 🧪 НОВЫЕ БИЗНЕС-ПРАВИЛА: ПРОШЕДШЕЕ СОБЫТИЕ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_ForStartedEvent_ShouldThrowEventAlreadyStartedException()
    {
        // Arrange — событие началось вчера
        var (eventItem, user) = await SetupAsync(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddDays(-2),
            endAt: DateTime.UtcNow.AddDays(-1));

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(eventItem.Id, user.Id));

        exception.Message.Should().Contain("уже началось");
    }

    [Fact]
    public async Task CreateBookingAsync_ForEventThatStartsNow_ShouldThrowEventAlreadyStartedException()
    {
        // Arrange — событие началось только что
        var (eventItem, user) = await SetupAsync(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddSeconds(-1),
            endAt: DateTime.UtcNow.AddHours(1));

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(eventItem.Id, user.Id));

        exception.Message.Should().Contain("уже началось");
    }

    [Fact]
    public async Task CreateBookingAsync_ForFutureEvent_ShouldSucceed()
    {
        // Arrange — событие начинается завтра
        var (eventItem, user) = await SetupAsync(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2));

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);

        // Assert
        booking.Should().NotBeNull();
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    // =============================================
    // 🧪 НОВЫЕ БИЗНЕС-ПРАВИЛА: ЛИМИТ БРОНЕЙ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitExceeded_ShouldThrowBookingLimitExceededException()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Создаём ровно 10 активных броней
        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user.Id);
        }

        // 11-е событие
        var extraEvent = new Event("Лишнее событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(extraEvent);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(extraEvent.Id, user.Id));

        exception.Message.Should().Contain("лимит");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitReached_ShouldNotCreateBooking()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user.Id);
        }

        var extraEvent = new Event("Лишнее событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(extraEvent);
        await context.SaveChangesAsync();

        var bookingsBefore = await context.Bookings.CountAsync();

        // Act
        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(extraEvent.Id, user.Id));

        // Assert — количество броней не изменилось
        var bookingsAfter = await context.Bookings.CountAsync();
        bookingsAfter.Should().Be(bookingsBefore);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingLimitReached_ShouldNotDecreaseAvailableSeats()
    {
        // Arrange
        var (_, user) = await SetupAsync();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user.Id);
        }

        var extraEvent = new Event("Лишнее событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(extraEvent);
        await context.SaveChangesAsync();

        var seatsBefore = (await context.Events.FindAsync(extraEvent.Id))!.AvailableSeats;

        // Act
        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            bookingService.CreateBookingAsync(extraEvent.Id, user.Id));

        // Assert — место не списалось
        var seatsAfter = (await context.Events.FindAsync(extraEvent.Id))!.AvailableSeats;
        seatsAfter.Should().Be(seatsBefore);
    }

    // =============================================
    // 🧪 ЛИМИТЫ РАЗНЫХ ПОЛЬЗОВАТЕЛЕЙ НЕЗАВИСИМЫ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_LimitsOfDifferentUsers_ShouldNotAffectEachOther()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user1 = new User($"user1_{Guid.NewGuid():N}", "hash");
        var user2 = new User($"user2_{Guid.NewGuid():N}", "hash");
        await context.Users.AddAsync(user1);
        await context.Users.AddAsync(user2);
        await context.SaveChangesAsync();

        // user1 бронирует 10 событий (достигает лимита)
        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user1.Id);
        }

        // Создаём ещё одно событие
        var newEvent = new Event("Новое событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(newEvent);
        await context.SaveChangesAsync();

        // Act — user2 должен успешно забронировать, его лимит не затронут
        var booking2 = await bookingService.CreateBookingAsync(newEvent.Id, user2.Id);

        // Assert
        booking2.Should().NotBeNull();
        booking2.UserId.Should().Be(user2.Id);
        booking2.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUser1AtLimit_User2CanStillBook()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user1 = new User($"user1_{Guid.NewGuid():N}", "hash");
        var user2 = new User($"user2_{Guid.NewGuid():N}", "hash");
        await context.Users.AddAsync(user1);
        await context.Users.AddAsync(user2);
        await context.SaveChangesAsync();

        // user1 — 10 броней
        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие1_{i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user1.Id);
        }

        // user2 — только 5 броней
        for (int i = 0; i < 5; i++)
        {
            var ev = new Event($"Событие2_{i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            await bookingService.CreateBookingAsync(ev.Id, user2.Id);
        }

        // Создаём ещё одно событие
        var newEvent = new Event("Новое", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(newEvent);
        await context.SaveChangesAsync();

        // Act
        var booking = await bookingService.CreateBookingAsync(newEvent.Id, user2.Id);

        // Assert — user2 может бронировать (у него только 6-я)
        booking.Should().NotBeNull();
        booking.UserId.Should().Be(user2.Id);
    }

    // =============================================
    // ✅ ПОЛУЧЕНИЕ БРОНИ
    // =============================================

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

    // =============================================
    // 🧪 ОТМЕНА БРОНИ
    // =============================================

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

    [Fact]
    public async Task CancelBookingAsync_ShouldReturnSeatToPool()
    {
        // Arrange
        var (eventItem, user) = await SetupAsync(5);

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventItem.Id, user.Id);
        var afterBooking = await context.Events.FindAsync(eventItem.Id);
        afterBooking!.AvailableSeats.Should().Be(4);

        // Act
        await bookingService.CancelBookingAsync(booking.Id, user.Id, UserRole.User);

        // Assert
        var afterCancel = await context.Events.FindAsync(eventItem.Id);
        afterCancel!.AvailableSeats.Should().Be(5);
    }

    [Fact]
    public async Task CancelBookingAsync_CancelledBooking_ShouldNotCountInUserLimit()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user = new User($"user_{Guid.NewGuid():N}", "hash");
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        // Создаём 10 броней, потом отменяем одну
        var eventIds = new List<Guid>();
        for (int i = 0; i < 10; i++)
        {
            var ev = new Event($"Событие {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            await context.Events.AddAsync(ev);
            await context.SaveChangesAsync();
            eventIds.Add(ev.Id);
        }

        var bookings = new List<Booking>();
        foreach (var id in eventIds)
        {
            bookings.Add(await bookingService.CreateBookingAsync(id, user.Id));
        }

        // Отменяем одну
        await bookingService.CancelBookingAsync(bookings[0].Id, user.Id, UserRole.User);

        // Создаём ещё одно событие
        var newEvent = new Event("Новое событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await context.Events.AddAsync(newEvent);
        await context.SaveChangesAsync();

        // Act — теперь у пользователя 9 активных, лимит не превышен
        var newBooking = await bookingService.CreateBookingAsync(newEvent.Id, user.Id);

        // Assert
        newBooking.Should().NotBeNull();
    }

    // =============================================
    // 🔧 Dispose
    // =============================================

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        context.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}