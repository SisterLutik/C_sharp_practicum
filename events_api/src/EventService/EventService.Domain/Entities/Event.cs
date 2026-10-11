namespace EventService.Domain.Entities;

public class Event
{
    // Приватный конструктор для EF Core
    private Event() { }

    // Публичный конструктор для создания события в коде
    public Event(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название события не может быть пустым", nameof(title));

        if (totalSeats <= 0)
            throw new ArgumentException("Количество мест должно быть больше 0", nameof(totalSeats));

        if (endAt <= startAt)
            throw new ArgumentException("Дата окончания должна быть позже даты начала", nameof(endAt));

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt.Kind == DateTimeKind.Utc
            ? startAt
            : DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        EndAt = endAt.Kind == DateTimeKind.Utc
            ? endAt
            : DateTime.SpecifyKind(endAt, DateTimeKind.Utc);
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }


    /// <summary>
    /// Проверяет, началось ли событие (сравнение с текущим временем UTC).
    /// </summary>
    public bool HasStarted() => StartAt <= DateTime.UtcNow;

    /// <summary>
    /// Пытается зарезервировать count мест.
    /// Возвращает false, если мест недостаточно.
    /// </summary>
    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0)
            throw new ArgumentException("Количество мест должно быть больше 0", nameof(count));

        if (AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Освобождает count мест (возврат в пул).
    /// </summary>
    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0)
            throw new ArgumentException("Количество мест должно быть больше 0", nameof(count));

        AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
    }

    /// <summary>
    /// Обновляет основные параметры события.
    /// При изменении TotalSeats пересчитывает AvailableSeats.
    /// </summary>
    public void UpdateDetails(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название события не может быть пустым", nameof(title));

        if (totalSeats <= 0)
            throw new ArgumentException("Количество мест должно быть больше 0", nameof(totalSeats));

        if (endAt <= startAt)
            throw new ArgumentException("Дата окончания должна быть позже даты начала", nameof(endAt));

        Title = title;
        Description = description;
        StartAt = startAt.Kind == DateTimeKind.Utc
            ? startAt
            : DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        EndAt = endAt.Kind == DateTimeKind.Utc
            ? endAt
            : DateTime.SpecifyKind(endAt, DateTimeKind.Utc);

        var oldTotalSeats = TotalSeats;
        TotalSeats = totalSeats;

        if (TotalSeats != oldTotalSeats)
        {
            // Если мест стало больше — добавляем разницу к доступным.
            // Если меньше — обрезаем доступные до нового TotalSeats.
            var seatsAdded = Math.Max(0, TotalSeats - oldTotalSeats);
            AvailableSeats = Math.Min(AvailableSeats + seatsAdded, TotalSeats);
        }
    }
}