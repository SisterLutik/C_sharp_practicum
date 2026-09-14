using System;

namespace EventsApi.Domain.Exceptions
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException(string message) : base(message) { }
    }
}