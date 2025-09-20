﻿using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class RateLimitExceededException : BaseException
    {
        public RateLimitExceededException(string message = "Limite de requisições excedido")
            : base(message, HttpStatusCode.TooManyRequests) { }

        protected RateLimitExceededException(string message, HttpStatusCode statusCode) : base(message, statusCode)
        {
        }

        public RateLimitExceededException() : base()
        {
        }

        public RateLimitExceededException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
