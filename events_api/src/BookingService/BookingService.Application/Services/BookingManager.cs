using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

public class BookingManager : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    private readonly IBookingRepository _bookingRepository;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly ILogger<BookingManager> _logger;
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingManager(
        IBookingRepository bookingRepository,
        IEventServiceClient eventServiceClient,
        ILogger<BookingManager> logger)
    {
        _bookingRepository = bookingRepository;
        _eventServiceClient = eventServiceClient;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            var eventDto = await _eventServiceClient.GetEventAsync(eventId);
            if (eventDto == null)
                throw new NotFoundException($"Событие с id {eventId} не найдено");

            if (eventDto.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyStartedException(
                    "Нельзя забронировать событие, которое уже началось");

            var activeBookings = await _bookingRepository.GetActiveByUserAsync(userId);
            if (activeBookings.Count >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Превышен лимит активных броней ({MaxActiveBookingsPerUser})");

            var reserveResult = await _eventServiceClient.TryReserveSeatsAsync(eventId);
            if (!reserveResult.Success)
                throw new NoAvailableSeatsException(
                    reserveResult.Reason ?? "No available seats for this event");

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

        var eventDto = await _eventServiceClient.GetEventAsync(booking.EventId);
        if (eventDto != null && eventDto.StartAt <= DateTime.UtcNow)
            throw new EventAlreadyStartedException(
                "Нельзя отменить бронь на событие, которое уже началось");

        booking.Cancel();
        await _bookingRepository.UpdateAsync(booking);

        await _eventServiceClient.ReleaseSeatsAsync(booking.EventId);

        _logger.LogInformation(
            $"Бронь {booking.Id} отменена пользователем {requestingUserId}");
    }
}