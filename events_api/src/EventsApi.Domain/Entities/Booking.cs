using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;

namespace EventsApi.Domain.Entities;

public class Booking
{
    // Приватный конструктор для EF Core
    private Booking() { }

    public Booking(Guid eventId, Guid userId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId не может быть пустым", nameof(eventId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId не может быть пустым", nameof(userId));

        Id = Guid.NewGuid();
        EventId = eventId;
        UserId = userId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public Event Event { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new ForbiddenOperationException("Подтвердить можно только бронь в статусе Pending");

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != BookingStatus.Pending)
            throw new ForbiddenOperationException("Отклонить можно только бронь в статусе Pending");

        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отмена брони пользователем.
    /// Защита от повторной отмены через проверку статуса.
    /// </summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException($"Бронь с id {Id} уже отменена");

        if (Status == BookingStatus.Rejected)
            throw new ForbiddenOperationException("Нельзя отменить отклонённую бронь");

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}