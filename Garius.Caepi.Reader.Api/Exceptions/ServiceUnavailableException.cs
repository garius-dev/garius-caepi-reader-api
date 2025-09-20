using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class ServiceUnavailableException : BaseException
    {
        public ServiceUnavailableException(string message = "Serviço temporariamente indisponível")
            : base(message, HttpStatusCode.ServiceUnavailable) { }

        protected ServiceUnavailableException(string message, HttpStatusCode statusCode) : base(message, statusCode)
        {
        }

        public ServiceUnavailableException() : base()
        {
        }

        public ServiceUnavailableException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
