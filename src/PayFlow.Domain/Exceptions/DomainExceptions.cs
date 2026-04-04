namespace PayFlow.Domain.Exceptions
{
    // Base exception for domain-related errors
    public class DomainException(string message) : Exception(message)
    {
    }

    // Custom exception for insufficient balance scenarios
    public class InsufficientBalanceException(string message) : DomainException(message)
    {
    }

    // Custom exception for invalid transfer scenarios
    public class InvalidTransferException(string message) : DomainException(message)
    {
    }
}