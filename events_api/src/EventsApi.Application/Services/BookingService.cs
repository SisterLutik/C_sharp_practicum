using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;

public class BookingService : IBookingService
{
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

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            var eventExists = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new NotFoundException($"Событие с id {eventId} не найдено");

            if (!eventExists.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            await _eventRepository.UpdateAsync(eventExists);

            var booking = new Booking(eventId);
            await _bookingRepository.AddAsync(booking);
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
}