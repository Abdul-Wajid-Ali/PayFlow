using System.Net;

namespace PayFlow.Application.Common.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public string Title { get; }

        public string Detail { get; }

        public int StatusCode { get; }

        public BusinessRuleException(
            string title,
            string detail,
            int statusCode = (int)HttpStatusCode.Conflict)
        : base(detail)
        {
            Title = title;
            Detail = detail;
            StatusCode = statusCode;
        }
    }
}