namespace PayFlow.Domain.Exceptions
{
    // Base exception for domain-related errors
    public class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }

    // Custom exception for insufficient balance scenarios
    public class InsufficientBalanceException : DomainException
    {
        public InsufficientBalanceException(string message) : base(message)
        {
        }
    }

    // Custom exception for invalid transfer scenarios
    public class InvalidTransferException : DomainException
    {
        public InvalidTransferException(string message) : base(message)
        {
        }
    }
}