using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using EventsApi.Contracts.Kafka;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

public class BookingManager : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    private readonly IBookingRepository _bookingRepository;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<BookingManager> _logger;
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingManager(
        IBookingRepository bookingRepository,
        IEventServiceClient eventServiceClient,
        IEventPublisher eventPublisher,
        ILogger<BookingManager> logger)
    {
        _bookingRepository = bookingRepository;
        _eventServiceClient = eventServiceClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    // ============================================
    // ✅ CreateBookingAsync(Guid, Guid)
    // ============================================
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            // 1. Событие существует?
            var eventDto = await _eventServiceClient.GetEventAsync(eventId);
            if (eventDto == null)
                throw new NotFoundException($"Событие с id {eventId} не найдено");

            // 2. Событие ещё не началось?
            if (eventDto.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyStartedException(
                    "Нельзя забронировать событие, которое уже началось");

            // 3. Лимит активных броней
            var activeBookings = await _bookingRepository.GetActiveByUserAsync(userId);
            if (activeBookings.Count >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Превышен лимит активных броней ({MaxActiveBookingsPerUser})");

            // 4. Резервируем место
            var reserveResult = await _eventServiceClient.TryReserveSeatsAsync(eventId);
            if (!reserveResult.Success)
                throw new NoAvailableSeatsException(
                    reserveResult.Reason ?? "No available seats for this event");

            // 5. Создаём бронь
            var booking = new Booking(eventId, userId);
            await _bookingRepository.AddAsync(booking);

            _logger.LogInformation(
                "Создана бронь {BookingId} для события {EventId} пользователем {UserId}",
                booking.Id, eventId, userId);

            return booking;
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    // ============================================
    // ✅ GetBookingByIdAsync(Guid)
    // ============================================
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        return booking;
    }

    // ============================================
    // ✅ CancelBookingAsync(Guid, Guid, bool)
    // ============================================
    public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        // Проверка прав: владелец или админ
        if (booking.UserId != requestingUserId && !isAdmin)
            throw new ForbiddenOperationException("Нет прав на отмену этой брони");

        // Проверка: событие ещё не началось
        var eventDto = await _eventServiceClient.GetEventAsync(booking.EventId);
        if (eventDto != null && eventDto.StartAt <= DateTime.UtcNow)
            throw new EventAlreadyStartedException(
                "Нельзя отменить бронь на событие, которое уже началось");

        // Доменный метод Cancel() защищает от повторной отмены
        booking.Cancel();
        await _bookingRepository.UpdateAsync(booking);

        // Возвращаем место в пул события
        await _eventServiceClient.ReleaseSeatsAsync(booking.EventId);

        _logger.LogInformation(
            "Бронь {BookingId} отменена пользователем {UserId}",
            bookingId, requestingUserId);
    }

    // ============================================
    // ✅ ConfirmBookingAsync(Guid)
    // ============================================
    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        booking.Confirm();

        // 1. Сначала сохраняем в свою БД
        await _bookingRepository.UpdateAsync(booking);

        // 2. Потом публикуем событие в Kafka
        var confirmedEvent = new BookingConfirmed(
            BookingId: booking.Id,
            EventId: booking.EventId,
            UserId: booking.UserId,
            SeatsCount: 1,
            ConfirmedAt: booking.ProcessedAt!.Value);

        await _eventPublisher.PublishBookingConfirmedAsync(confirmedEvent);

        _logger.LogInformation(
            "Бронь {BookingId} подтверждена и опубликована в Kafka", bookingId);
    }
}