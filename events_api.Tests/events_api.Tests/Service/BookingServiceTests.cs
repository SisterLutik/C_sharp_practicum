using events_api.Data;
using events_api.events_api.Exceptions;
using events_api.Interfaces;
using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace events_api.Tests.Services
{
    public class BookingServiceTests
    {
        // =============================================
        // ✅ УСПЕШНЫЕ СЦЕНАРИИ (с реальным репозиторием)
        // =============================================

        [Fact]
        public async Task CreateBookingAsync_WithExistingEvent_ShouldReturnBookingWithPendingStatus()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act
            var result = await bookingService.CreateBookingAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().Be(eventId);
            result.Status.Should().Be(BookingStatus.Pending);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ProcessedAt.Should().BeNull();
            result.Id.Should().NotBeEmpty();
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldReturnUniqueIds()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act
            var booking1 = await bookingService.CreateBookingAsync(eventId);
            var booking2 = await bookingService.CreateBookingAsync(eventId);
            var booking3 = await bookingService.CreateBookingAsync(eventId);

            // Assert
            booking1.Id.Should().NotBeEmpty();
            booking2.Id.Should().NotBeEmpty();
            booking3.Id.Should().NotBeEmpty();
            booking1.Id.Should().NotBe(booking2.Id);
            booking1.Id.Should().NotBe(booking3.Id);
            booking2.Id.Should().NotBe(booking3.Id);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithExistingId_ShouldReturnCorrectBooking()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act — создаём бронь
            var createdBooking = await bookingService.CreateBookingAsync(eventId);

            // Получаем её по Id
            var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(createdBooking.Id);
            result.EventId.Should().Be(eventId);
            result.Status.Should().Be(BookingStatus.Pending);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ProcessedAt.Should().BeNull();
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReflectStatusChangeAfterConfirm()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act — создаём бронь
            var booking = await bookingService.CreateBookingAsync(eventId);

            // Проверяем, что статус Pending
            var pendingBooking = await bookingService.GetBookingByIdAsync(booking.Id);
            pendingBooking!.Status.Should().Be(BookingStatus.Pending);

            // Меняем статус вручную (имитация фонового сервиса)
            booking.Status = BookingStatus.Confirmed;
            booking.ProcessedAt = DateTime.UtcNow;
            realRepository.Update(booking);

            // Получаем обновлённую бронь
            var confirmedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert
            confirmedBooking.Should().NotBeNull();
            confirmedBooking!.Status.Should().Be(BookingStatus.Confirmed);
            confirmedBooking.ProcessedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReflectStatusChangeAfterReject()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act — создаём бронь
            var booking = await bookingService.CreateBookingAsync(eventId);

            // Проверяем, что статус Pending
            var pendingBooking = await bookingService.GetBookingByIdAsync(booking.Id);
            pendingBooking!.Status.Should().Be(BookingStatus.Pending);

            // Меняем статус вручную (имитация фонового сервиса)
            booking.Status = BookingStatus.Rejected;
            booking.ProcessedAt = DateTime.UtcNow;
            realRepository.Update(booking);

            // Получаем обновлённую бронь
            var rejectedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert
            rejectedBooking.Should().NotBeNull();
            rejectedBooking!.Status.Should().Be(BookingStatus.Rejected);
            rejectedBooking.ProcessedAt.Should().NotBeNull();
        }

        // =============================================
        // ❌ НЕУСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public async Task CreateBookingAsync_WithNonExistingEvent_ShouldThrowBusinessException()
        {
            // Arrange
            var eventId = 999;
            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.GetById(eventId)).Returns((Event?)null);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                bookingService.CreateBookingAsync(eventId));

            exception.Message.Should().Be($"Событие с id {eventId} не найдено");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateBookingAsync_ForDeletedEvent_ShouldThrowBusinessException()
        {
            // Arrange
            var eventId = 1;
            var mockEventService = new Mock<IEventService>();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };

            // Событие существует при первом вызове
            mockEventService.Setup(s => s.GetById(eventId)).Returns(existingEvent);

            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act — создаём бронь
            var booking = await bookingService.CreateBookingAsync(eventId);
            booking.Should().NotBeNull();
            booking.Id.Should().NotBeEmpty();

            // Удаляем событие (имитация удаления)
            mockEventService.Setup(s => s.GetById(eventId)).Returns((Event?)null);

            // Пытаемся создать ещё одну бронь для удалённого события
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                bookingService.CreateBookingAsync(eventId));

            // Assert
            exception.Message.Should().Be($"Событие с id {eventId} не найдено");
            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var mockEventService = new Mock<IEventService>();
            var realRepository = new BookingRepository();
            var bookingService = new BookingService(realRepository, mockEventService.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                bookingService.GetBookingByIdAsync(nonExistingId));

            exception.Message.Should().Be($"Бронь с id {nonExistingId} не найдена");
            exception.StatusCode.Should().Be(404);
        }
    }
}