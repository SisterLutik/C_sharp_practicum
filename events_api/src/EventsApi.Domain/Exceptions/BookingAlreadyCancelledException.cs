namespace EventsApi.Domain.Exceptions;

public class BookingAlreadyCancelledException : Exception
{
    public BookingAlreadyCancelledException(string message) : base(message) { }
}