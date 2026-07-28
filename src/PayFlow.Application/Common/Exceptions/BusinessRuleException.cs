namespace PayFlow.Application.Common.Exceptions
{
    public class BusinessRuleException(
        string title,
        string detail,
        int statusCode = (int)HttpStatusCode.Conflict) : Exception(detail)
    {
        public string Title { get; } = title;

        public string Detail { get; } = detail;

        public int StatusCode { get; } = statusCode;
    }
}
