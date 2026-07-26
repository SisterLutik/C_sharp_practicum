using events_api.Data;
using events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace events_api.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly IBookingRepository _repository;
        private readonly EventService _eventService;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _repository = new BookingRepository(NullLogger<BookingRepository>.Instance);
            _eventService = new EventService();
            _bookingService = new BookingService(_repository, _eventService);
        }

        // =============================================
        // ✅ УСПЕШНЫЕ СЦЕНАРИИ (МЕСТА)
        // =============================================

        [Fact]
        public void CreateBooking_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventWithSeats = CreateTestEvent(eventId, 10);
            _eventService.Add(eventWithSeats);

            // Act
            var booking = _bookingService.CreateBooking(eventId);

            // Assert
            booking.Should().NotBeNull();
            var updatedEvent = _eventService.GetById(eventId);
            updatedEvent!.AvailableSeats.Should().Be(9);
        }

        [Fact]
        public void CreateBooking_MultipleBookingsUpToLimit_ShouldAllSucceed()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 5;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _eventService.Add(eventWithSeats);

            var bookingIds = new List<Guid>();

            // Act
            for (int i = 0; i < totalSeats; i++)
            {
                var booking = _bookingService.CreateBooking(eventId);
                bookingIds.Add(booking.Id);
            }

            // Assert
            bookingIds.Should().HaveCount(totalSeats);
            bookingIds.Should().OnlyHaveUniqueItems();
            var updatedEvent = _eventService.GetById(eventId);
            updatedEvent!.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public void CreateBooking_WhenSeatsExhausted_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 1;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _eventService.Add(eventWithSeats);

            // Act — создаём первую бронь
            var firstBooking = _bookingService.CreateBooking(eventId);

            // Assert
            firstBooking.Should().NotBeNull();
            var updatedEvent = _eventService.GetById(eventId);
            updatedEvent!.AvailableSeats.Should().Be(0);

            // Act & Assert — вторая попытка должна выбросить исключение
            var exception = Assert.Throws<NoAvailableSeatsException>(() =>
                _bookingService.CreateBooking(eventId));

            exception.Message.Should().Be("No available seats for this event");
        }

        // =============================================
        // ❌ НЕУСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public void CreateBooking_WithNonExistingEvent_ShouldThrowBusinessException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                _bookingService.CreateBooking(nonExistingId));

            exception.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public void CreateBooking_WithNoAvailableSeats_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 0;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _eventService.Add(eventWithSeats);

            // Act & Assert
            var exception = Assert.Throws<NoAvailableSeatsException>(() =>
                _bookingService.CreateBooking(eventId));

            exception.Message.Should().Be("No available seats for this event");
        }

        // =============================================
        // 🧪 ТЕСТЫ НА СМЕНУ СТАТУСА
        // =============================================

        [Fact]
        public void Confirm_ShouldSetStatusToConfirmedAndSetProcessedAt()
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            booking.Confirm();

            booking.Status.Should().Be(BookingStatus.Confirmed);
            booking.ProcessedAt.Should().NotBeNull();
            booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Reject_ShouldSetStatusToRejectedAndSetProcessedAt()
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            booking.Reject();

            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.ProcessedAt.Should().NotBeNull();
            booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ReleaseSeats_ShouldRestoreAvailableSeatsAfterReject()
        {
            var eventWithSeats = CreateTestEvent(Guid.NewGuid(), 10);
            var initialSeats = eventWithSeats.AvailableSeats;

            eventWithSeats.TryReserveSeats();
            var seatsAfterReserve = eventWithSeats.AvailableSeats;

            eventWithSeats.ReleaseSeats();
            var seatsAfterRelease = eventWithSeats.AvailableSeats;

            seatsAfterReserve.Should().Be(initialSeats - 1);
            seatsAfterRelease.Should().Be(initialSeats);
        }

        [Fact]
        public void AfterReject_ShouldBeAbleToCreateNewBookingForSameSpot()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventWithSeats = CreateTestEvent(eventId, 1);
            _eventService.Add(eventWithSeats);

            // Act — создаём бронь
            var booking = _bookingService.CreateBooking(eventId);
            var updatedEvent = _eventService.GetById(eventId);
            updatedEvent!.AvailableSeats.Should().Be(0);

            // Act — отменяем бронь (освобождаем место)
            booking.Reject();
            _repository.Update(booking);
            updatedEvent.ReleaseSeats();

            var eventAfterRelease = _eventService.GetById(eventId);
            eventAfterRelease!.AvailableSeats.Should().Be(1);

            // Act — создаём новую бронь
            var newBooking = _bookingService.CreateBooking(eventId);

            // Assert
            newBooking.Should().NotBeNull();
            newBooking.Id.Should().NotBe(booking.Id);
            newBooking.Status.Should().Be(BookingStatus.Pending);
            var finalEvent = _eventService.GetById(eventId);
            finalEvent!.AvailableSeats.Should().Be(0);
        }

        // =============================================
        // 🧪 ТЕСТЫ НА КОНКУРЕНТНОСТЬ
        // =============================================

        [Fact]
        public void ConcurrentBooking_With5SeatsAnd20Requests_ShouldSucceedExactly5AndThrow15()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 5;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _eventService.Add(eventWithSeats);

            var results = new List<Booking>();
            var exceptions = new List<Exception>();

            // Act
            Parallel.For(0, 20, i =>
            {
                try
                {
                    var booking = _bookingService.CreateBooking(eventId);
                    lock (results)
                    {
                        results.Add(booking);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // Assert
            results.Should().HaveCount(totalSeats);
            results.Select(b => b.Id).Should().OnlyHaveUniqueItems();
            exceptions.Should().HaveCount(15);
            exceptions.Should().AllBeOfType<NoAvailableSeatsException>();
            var finalEvent = _eventService.GetById(eventId);
            finalEvent!.AvailableSeats.Should().Be(0);
        }

        [Fact]
        public void ConcurrentBooking_With10SeatsAnd10Requests_ShouldAllSucceedWithUniqueIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var totalSeats = 10;
            var eventWithSeats = CreateTestEvent(eventId, totalSeats);
            _eventService.Add(eventWithSeats);

            var results = new List<Booking>();

            // Act
            Parallel.For(0, totalSeats, i =>
            {
                var booking = _bookingService.CreateBooking(eventId);
                lock (results)
                {
                    results.Add(booking);
                }
            });

            var bookingIds = results.Select(b => b.Id).ToList();

            // Assert
            results.Should().HaveCount(totalSeats);
            bookingIds.Should().OnlyHaveUniqueItems();
            var finalEvent = _eventService.GetById(eventId);
            finalEvent!.AvailableSeats.Should().Be(0);
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