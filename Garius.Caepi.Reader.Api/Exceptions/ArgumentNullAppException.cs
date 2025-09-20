using System.Net;

namespace Garius.Caepi.Reader.Api.Exceptions
{
    public class ArgumentNullAppException : BaseException
    {
        public string? ParamName { get; }

        public ArgumentNullAppException(string paramName)
            : base($"O argumento '{paramName}' não pode ser nulo.", HttpStatusCode.BadRequest)
        {
            ParamName = paramName;
        }

        public ArgumentNullAppException(string paramName, string message)
            : base(message, HttpStatusCode.BadRequest)
        {
            ParamName = paramName;
        }

        public ArgumentNullAppException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
