namespace EventService.Application.DTOs;

public class EventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Процент проданных мест (0.0 – 1.0).
    /// Полезно показывать в топе.
    /// </summary>
    public double SalesPercentage =>
        TotalSeats == 0
            ? 0
            : (double)(TotalSeats - AvailableSeats) / TotalSeats;
}