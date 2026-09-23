namespace EventsApi.Domain.Exceptions;

public class EventAlreadyStartedException : Exception
{
    public EventAlreadyStartedException(string message) : base(message) { }
}