namespace PayFlow.Application.Common.Exceptions
{
    public class IdempotencyConflictException(string message) : Exception(message)
    {
    }
}