using System;

namespace events_api.Exceptions
{ 
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException(string message) : base(message) { }
    }
}