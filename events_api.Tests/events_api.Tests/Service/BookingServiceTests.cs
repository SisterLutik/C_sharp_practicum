using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;

public class BookingService : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingService> _logger;
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository,
        ILogger<BookingService> logger)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            // 1. Событие существует?
            var eventEntity = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new NotFoundException($"Событие с id {eventId} не найдено");

            // 2. Событие ещё не началось? (иначе 400)
            if (eventEntity.HasStarted())
                throw new EventAlreadyStartedException(
                    "Нельзя забронировать событие, которое уже началось");

            // 3. Лимит активных броней не превышен? (иначе 409)
            var activeBookings = await _bookingRepository.GetActiveByUserAsync(userId);
            if (activeBookings.Count >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Превышен лимит активных броней ({MaxActiveBookingsPerUser})");

            // 4. Свободные места есть? (иначе 409)
            if (!eventEntity.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            await _eventRepository.UpdateAsync(eventEntity);

            // 5. Создаём бронь с userId
            var booking = new Booking(eventId, userId);
            await _bookingRepository.AddAsync(booking);

            _logger.LogInformation(
                $"Создана бронь {booking.Id} для события {eventId} пользователем {userId}");
            return booking;
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid requestingUserId, UserRole role)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        // Проверка прав: владелец или админ (иначе 403)
        if (booking.UserId != requestingUserId && role != UserRole.Admin)
            throw new ForbiddenOperationException("Нет прав на отмену этой брони");

        // Доменный метод Cancel() защищает от повторной отмены
        booking.Cancel();

        // Возвращаем место в пул
        var eventEntity = await _eventRepository.GetByIdAsync(booking.EventId);
        if (eventEntity != null)
        {
            eventEntity.ReleaseSeats();
            await _eventRepository.UpdateAsync(eventEntity);
        }

        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation(
            $"Бронь {booking.Id} отменена пользователем {requestingUserId}");
    }
}