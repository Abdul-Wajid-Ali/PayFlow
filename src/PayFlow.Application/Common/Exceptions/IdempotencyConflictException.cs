namespace PayFlow.Application.Common.Exceptions
{
    public class IdempotencyConflictException : Exception
    {
        public IdempotencyConflictException(string message) : base(message)
        {
        }
    }
}