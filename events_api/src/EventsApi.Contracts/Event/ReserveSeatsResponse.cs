namespace EventsApi.Contracts.Events;

public class ReserveSeatsResponse
{
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public int AvailableSeats { get; set; }
}