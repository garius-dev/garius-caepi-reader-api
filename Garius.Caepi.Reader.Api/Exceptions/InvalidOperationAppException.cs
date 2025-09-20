﻿using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class InvalidOperationAppException : BaseException
    {
        public InvalidOperationAppException(string message = "Operação inválida")
            : base(message, HttpStatusCode.BadRequest) { }

        protected InvalidOperationAppException(string message, HttpStatusCode statusCode)
            : base(message, statusCode)
        {
        }

        public InvalidOperationAppException() : base()
        {
        }

        public InvalidOperationAppException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
