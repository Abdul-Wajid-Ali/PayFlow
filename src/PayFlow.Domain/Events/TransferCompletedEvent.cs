namespace PayFlow.Domain.Events
{
    public record TransferCompletedEvent(
        Guid TransactionId,
        Guid FromWalletId,
        Guid ToWalletId,
        decimal Amount,
        string Currency,
        DateTime CompletedAt);
}