using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class ConflictException : BaseException
    {
        public ConflictException(string message = "Conflito de dados")
            : base(message, HttpStatusCode.Conflict) { }

        protected ConflictException(string message, HttpStatusCode statusCode) : base(message, statusCode)
        {
        }

        public ConflictException() : base()
        {
        }

        public ConflictException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
