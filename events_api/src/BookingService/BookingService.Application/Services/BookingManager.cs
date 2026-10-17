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
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<BookingManager> _logger;
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingManager(
        IBookingRepository bookingRepository,
        IEventPublisher eventPublisher,
        ILogger<BookingManager> logger)
    {
        _bookingRepository = bookingRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            // Только проверка лимита пользователя
            var activeBookings = await _bookingRepository.GetActiveByUserAsync(userId);
            if (activeBookings.Count >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Превышен лимит активных броней ({MaxActiveBookingsPerUser})");

            // Создаём бронь без обращения к EventService
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

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        if (booking.UserId != requestingUserId && !isAdmin)
            throw new ForbiddenOperationException("Нет прав на отмену этой брони");

        booking.Cancel();
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation(
            "Бронь {BookingId} отменена пользователем {UserId}",
            bookingId, requestingUserId);
    }

    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        if (booking.Status != BookingStatus.Pending)
            throw new ForbiddenOperationException(
                "Подтвердить можно только бронь в статусе Pending");

        // 1. Сохраняем в свою БД
        booking.Confirm();
        await _bookingRepository.UpdateAsync(booking);

        // 2. Публикуем в Kafka — EventService сам решит, уменьшать ли места
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