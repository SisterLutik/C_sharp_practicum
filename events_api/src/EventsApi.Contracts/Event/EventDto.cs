namespace EventsApi.Contracts.Events;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}