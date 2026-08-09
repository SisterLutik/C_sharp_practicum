using events_api.DataAccess;
using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace events_api.Tests.Services
{
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

            services.AddSingleton<ILogger<BookingService>>(NullLogger<BookingService>.Instance);
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            // Act
            var booking = await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            booking.Should().NotBeNull();
            var updatedEvent = await context.Events.FindAsync(eventItem.Id);
            updatedEvent!.AvailableSeats.Should().Be(9);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsUpToLimit_ShouldAllSucceed()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var totalSeats = 5;
            var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), totalSeats);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var bookingIds = new List<Guid>();

            // Act
            for (int i = 0; i < totalSeats; i++)
            {
                var booking = await bookingService.CreateBookingAsync(eventItem.Id);
                bookingIds.Add(booking.Id);
            }

            // Assert
            bookingIds.Should().HaveCount(totalSeats);
            bookingIds.Should().OnlyHaveUniqueItems();
            var updatedEvent = await context.Events.FindAsync(eventItem.Id);
            updatedEvent!.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsExhausted_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var totalSeats = 1;
            var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), totalSeats);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            // Act
            var firstBooking = await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            firstBooking.Should().NotBeNull();
            var updatedEvent = await context.Events.FindAsync(eventItem.Id);
            updatedEvent!.AvailableSeats.Should().Be(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                bookingService.CreateBookingAsync(eventItem.Id));
            exception.Message.Should().Be("No available seats for this event");
        }

        [Fact]
        public async Task CreateBookingAsync_WithNonExistingEvent_ShouldThrowBusinessException()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                bookingService.CreateBookingAsync(nonExistingId));
            exception.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithExistingId_ShouldReturnBooking()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var booking = await bookingService.CreateBookingAsync(eventItem.Id);

            // Act
            var result = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(booking.Id);
            result.EventId.Should().Be(eventItem.Id);
            result.Status.Should().Be(BookingStatus.Pending);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                bookingService.GetBookingByIdAsync(nonExistingId));
            exception.Message.Should().Be($"Бронь с id {nonExistingId} не найдена");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public void Confirm_ShouldSetStatusToConfirmedAndSetProcessedAt()
        {
            // Arrange
            var booking = new Booking(Guid.NewGuid());

            // Act
            booking.Confirm();

            // Assert
            booking.Status.Should().Be(BookingStatus.Confirmed);
            booking.ProcessedAt.Should().NotBeNull();
            booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Reject_ShouldSetStatusToRejectedAndSetProcessedAt()
        {
            // Arrange
            var booking = new Booking(Guid.NewGuid());

            // Act
            booking.Reject();

            // Assert
            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.ProcessedAt.Should().NotBeNull();
            booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ConcurrentBooking_With5SeatsAnd20Requests_ShouldSucceedExactly5AndThrow15()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eventItem = new Event("Событие на 5 мест", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 5);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var results = new List<Booking>();
            var exceptions = new List<Exception>();

            // Act — каждый запрос получает свой scope
            var tasks = Enumerable.Range(0, 20).Select(async _ =>
            {
                using var childScope = _serviceProvider.CreateScope();
                var bookingService = childScope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    var booking = await bookingService.CreateBookingAsync(eventItem.Id);
                    lock (results) results.Add(booking);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });

            await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(5);
            results.Select(b => b.Id).Should().OnlyHaveUniqueItems();
            exceptions.Should().HaveCount(15);
            exceptions.Should().AllBeOfType<NoAvailableSeatsException>();

            using var verifyScope = _serviceProvider.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedEvent = await verifyContext.Events.FindAsync(eventItem.Id);
            updatedEvent!.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public async Task ConcurrentBooking_With10SeatsAnd10Requests_ShouldAllSucceedWithUniqueIds()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eventItem = new Event("Событие на 10 мест", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var results = new List<Booking>();

            // Act — каждый запрос получает свой scope
            var tasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                using var childScope = _serviceProvider.CreateScope();
                var bookingService = childScope.ServiceProvider.GetRequiredService<IBookingService>();
                var booking = await bookingService.CreateBookingAsync(eventItem.Id);
                lock (results) results.Add(booking);
            });

            await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Select(b => b.Id).Should().OnlyHaveUniqueItems();

            using var verifyScope = _serviceProvider.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedEvent = await verifyContext.Events.FindAsync(eventItem.Id);
            updatedEvent!.AvailableSeats.Should().Be(0);
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
}