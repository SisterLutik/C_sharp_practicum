using events_api.Data;
using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace events_api.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly IBookingRepository _repository;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _repository = new BookingRepository(NullLogger<BookingRepository>.Instance);
            _mockEventService = new Mock<IEventService>();
            _bookingService = new BookingService(_repository, _mockEventService.Object);
        }

        // =============================================
        // ✅ УСПЕШНЫЕ СЦЕНАРИИ (МЕСТА)
        // =============================================

        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventWithSeats = CreateTestEvent(eventId, 10);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            // Act
            var booking = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            booking.Should().NotBeNull();
            eventWithSeats.AvailableSeats.Should().Be(9);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsUpToLimit_ShouldAllSucceed()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 5;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            var bookingIds = new List<Guid>();

            // Act
            for (int i = 0; i < totalSeats; i++)
            {
                var booking = await _bookingService.CreateBookingAsync(eventId);
                bookingIds.Add(booking.Id);
            }

            // Assert
            bookingIds.Should().HaveCount(totalSeats);
            bookingIds.Should().OnlyHaveUniqueItems();
            eventWithSeats.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsExhausted_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 1;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            // Act — создаём первую бронь
            var firstBooking = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            firstBooking.Should().NotBeNull();
            eventWithSeats.AvailableSeats.Should().Be(0);

            // Act & Assert — вторая попытка должна выбросить исключение
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(eventId));

            exception.Message.Should().Be($"Нет свободных мест для события {eventId}");
        }

        // =============================================
        // ❌ НЕУСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public async Task CreateBookingAsync_WithNonExistingEvent_ShouldThrowBusinessException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            _mockEventService.Setup(s => s.GetById(nonExistingId)).Returns((Event?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                _bookingService.CreateBookingAsync(nonExistingId));

            exception.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateBookingAsync_WithNoAvailableSeats_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 0;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(eventId));

            exception.Message.Should().Be($"Нет свободных мест для события {eventId}");
        }

        // =============================================
        // 🧪 ТЕСТЫ НА СМЕНУ СТАТУСА
        // =============================================

        [Fact]
        public void Confirm_ShouldSetStatusToConfirmedAndSetProcessedAt()
        {
            // Arrange
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

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
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            // Act
            booking.Reject();

            // Assert
            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.ProcessedAt.Should().NotBeNull();
            booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ReleaseSeats_ShouldRestoreAvailableSeatsAfterReject()
        {
            // Arrange
            var eventWithSeats = CreateTestEvent(Guid.NewGuid(), 10);
            var initialSeats = eventWithSeats.AvailableSeats;

            // Act — резервируем место
            var reserveResult = eventWithSeats.TryReserveSeats();
            var seatsAfterReserve = eventWithSeats.AvailableSeats;

            // Act — освобождаем место
            eventWithSeats.ReleaseSeats();
            var seatsAfterRelease = eventWithSeats.AvailableSeats;

            // Assert
            reserveResult.Should().BeTrue();
            seatsAfterReserve.Should().Be(initialSeats - 1);
            seatsAfterRelease.Should().Be(initialSeats);
        }

        [Fact]
        public async Task AfterReject_ShouldBeAbleToCreateNewBookingForSameSpot()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventWithSeats = CreateTestEvent(eventId, 1);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            // Act — создаём бронь
            var booking = await _bookingService.CreateBookingAsync(eventId);
            eventWithSeats.AvailableSeats.Should().Be(0);

            // Act — отменяем бронь (освобождаем место)
            booking.Reject();
            _repository.Update(booking);
            eventWithSeats.ReleaseSeats();

            // Проверяем, что место освободилось
            eventWithSeats.AvailableSeats.Should().Be(1);

            // Act — создаём новую бронь
            var newBooking = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            newBooking.Should().NotBeNull();
            newBooking.Id.Should().NotBe(booking.Id);
            newBooking.Status.Should().Be(BookingStatus.Pending);
            eventWithSeats.AvailableSeats.Should().Be(0);
        }

        // =============================================
        // 🧪 ТЕСТЫ НА КОНКУРЕНТНОСТЬ
        // =============================================

        [Fact]
        public async Task ConcurrentBooking_With5SeatsAnd20Requests_ShouldSucceedExactly5AndThrow15()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 5;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            // Act
            var tasks = new List<Task<Booking>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(_bookingService.CreateBookingAsync(eventId));
            }

            var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(r => r)));
            var exceptions = results.Where(r => r.IsFaulted).Select(r => r.Exception?.InnerException).ToList();
            var successful = results.Where(r => r.IsCompletedSuccessfully).Select(r => r.Result).ToList();

            // Assert
            successful.Should().HaveCount(totalSeats);
            successful.Select(b => b.Id).Should().OnlyHaveUniqueItems();
            exceptions.Should().HaveCount(15);
            exceptions.Should().AllBeOfType<NoAvailableSeatsException>();
            eventWithSeats.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public async Task ConcurrentBooking_With10SeatsAnd10Requests_ShouldAllSucceedWithUniqueIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 10;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);

            _mockEventService.Setup(s => s.GetById(eventId)).Returns(eventWithSeats);
            _mockEventService.Setup(s => s.Update(eventId, It.IsAny<Event>()))
                .Callback<Guid, Event>((id, updatedEvent) =>
                {
                    eventWithSeats.AvailableSeats = updatedEvent.AvailableSeats;
                });

            // Act
            var tasks = new List<Task<Booking>>();
            for (int i = 0; i < totalSeats; i++)
            {
                tasks.Add(_bookingService.CreateBookingAsync(eventId));
            }

            var results = await Task.WhenAll(tasks);
            var bookingIds = results.Select(b => b.Id).ToList();

            // Assert
            results.Should().HaveCount(totalSeats);
            bookingIds.Should().OnlyHaveUniqueItems();
            eventWithSeats.AvailableSeats.Should().Be(0);
        }

        // =============================================
        // 🔧 ВСПОМОГАТЕЛЬНЫЙ МЕТОД
        // =============================================

        private Event CreateTestEvent(Guid eventId, int totalSeats)
        {
            return new Event
            {
                Id = eventId,
                Title = $"Тестовое событие на {totalSeats} мест",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }
    }
}