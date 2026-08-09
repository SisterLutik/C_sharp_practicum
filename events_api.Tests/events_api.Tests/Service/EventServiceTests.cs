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
    public class EventServiceTests : IDisposable
    {
        private readonly string _dbName;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventService _eventService;
        private readonly AppDbContext _context;

        public EventServiceTests()
        {
            _dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddSingleton<ILogger<EventService>>(NullLogger<EventService>.Instance);
            services.AddScoped<IEventService, EventService>();

            _serviceProvider = services.BuildServiceProvider();
            _eventService = _serviceProvider.GetRequiredService<IEventService>();
            _context = _serviceProvider.GetRequiredService<AppDbContext>();
        }

        [Fact]
        public async Task CreateEventAsync_ShouldCreateNewEvent()
        {
            // Arrange
            var title = "Новое событие";
            var description = "Описание события";
            var startAt = DateTime.Now.AddDays(1);
            var endAt = DateTime.Now.AddDays(2);
            var totalSeats = 10;

            // Act
            var result = await _eventService.CreateEventAsync(title, description, startAt, endAt, totalSeats);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);
            result.Title.Should().Be(title);
            result.Description.Should().Be(description);
            result.StartAt.Should().Be(startAt);
            result.EndAt.Should().Be(endAt);
            result.TotalSeats.Should().Be(totalSeats);
            result.AvailableSeats.Should().Be(totalSeats);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnEvent()
        {
            // Arrange
            var eventItem = new Event("Тестовое событие", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 20, "Описание");
            await _context.Events.AddAsync(eventItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetByIdAsync(eventItem.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(eventItem.Id);
            result.Title.Should().Be("Тестовое событие");
            result.Description.Should().Be("Описание");
            result.TotalSeats.Should().Be(20);
            result.AvailableSeats.Should().Be(20);
        }

        [Fact]
        public async Task UpdateAsync_WithExistingId_ShouldUpdateEvent()
        {
            // Arrange
            var eventItem = new Event("Старое название", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10, "Старое описание");
            await _context.Events.AddAsync(eventItem);
            await _context.SaveChangesAsync();

            var updatedEvent = new Event("Новое название", DateTime.Now.AddDays(3), DateTime.Now.AddDays(4), 20, "Новое описание");

            // Act
            var result = await _eventService.UpdateAsync(eventItem.Id, updatedEvent);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Новое название");
            result.Description.Should().Be("Новое описание");
            result.StartAt.Should().Be(updatedEvent.StartAt);
            result.EndAt.Should().Be(updatedEvent.EndAt);
            result.TotalSeats.Should().Be(20);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldRemoveEvent()
        {
            // Arrange
            var eventItem = new Event("Событие для удаления", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 5, "Описание");
            await _context.Events.AddAsync(eventItem);
            await _context.SaveChangesAsync();

            // Act
            await _eventService.DeleteAsync(eventItem.Id);
            var result = await _context.Events.FindAsync(eventItem.Id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_WithTitleFilter_ShouldReturnMatchingEvents()
        {
            // Arrange
            var event1 = new Event("Уникальное название", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10, "Описание 1");
            var event2 = new Event("Другое событие", DateTime.Now.AddDays(3), DateTime.Now.AddDays(4), 15, "Описание 2");
            await _context.Events.AddRangeAsync(event1, event2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetAllAsync("уникальное", null, null, 1, 10);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Уникальное название");
        }

        [Fact]
        public async Task GetAllAsync_WithDateFilters_ShouldReturnEventsInRange()
        {
            // Arrange
            var baseDate = new DateTime(2025, 7, 15);
            var event1 = new Event("Событие в диапазоне", baseDate.AddDays(-5), baseDate.AddDays(5), 10);
            var event2 = new Event("Событие вне диапазона", baseDate.AddDays(-20), baseDate.AddDays(-15), 10);
            await _context.Events.AddRangeAsync(event1, event2);
            await _context.SaveChangesAsync();

            var from = baseDate.AddDays(-10);
            var to = baseDate.AddDays(10);

            // Act
            var result = await _eventService.GetAllAsync(null, from, to, 1, 10);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Событие в диапазоне");
        }

        [Fact]
        public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var events = new List<Event>();
            for (int i = 0; i < 25; i++)
            {
                events.Add(new Event($"Событие {i}", DateTime.Now.AddDays(i), DateTime.Now.AddDays(i + 1), 10));
            }
            await _context.Events.AddRangeAsync(events);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetAllAsync(null, null, null, 2, 10);

            // Assert
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Items.Count.Should().Be(10);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(25);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidDates_ShouldThrowBusinessException()
        {
            // Arrange
            var eventItem = new Event("Событие с корректными датами", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);
            await _context.Events.AddAsync(eventItem);
            await _context.SaveChangesAsync();

            var invalidEvent = new Event("Некорректное событие", DateTime.Now.AddDays(3), DateTime.Now.AddDays(2), 10);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                _eventService.UpdateAsync(eventItem.Id, invalidEvent));

            exception.Message.Should().Be("EndAt должен быть позже StartAt");
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updatedEvent = new Event("Новое название", DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 10);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                _eventService.UpdateAsync(nonExistingId, updatedEvent));

            exception.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                _eventService.DeleteAsync(nonExistingId));

            exception.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}