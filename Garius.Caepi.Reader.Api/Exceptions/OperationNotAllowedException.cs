﻿using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class OperationNotAllowedException : BaseException
    {
        public OperationNotAllowedException(string message = "Operação não permitida")
            : base(message, HttpStatusCode.MethodNotAllowed) { }

        protected OperationNotAllowedException(string message, HttpStatusCode statusCode) : base(message, statusCode)
        {
        }

        public OperationNotAllowedException() : base()
        {
        }

        public OperationNotAllowedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
