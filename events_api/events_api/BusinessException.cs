namespace events_api
{
    using System;

    namespace events_api.Exceptions
    {
        public class BusinessException : Exception
        {
            public int StatusCode { get; }

            public BusinessException(string message, int statusCode = 400)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }
    }
}
