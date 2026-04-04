namespace PayFlow.Domain.Exceptions
{
    // Custom exception for non-recoverable transfer scenarios
    public class NonRecoverableTransferException(string message) : Exception(message)
    {
    }
}