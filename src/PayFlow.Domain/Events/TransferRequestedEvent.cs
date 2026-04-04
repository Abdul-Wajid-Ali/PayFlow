namespace PayFlow.Domain.Events
{
    public record TransferRequestedEvent(
        Guid TransactionId,
        Guid FromWalletId,
        Guid ToWalletId,
        decimal Amount,
        string Currency,
        string IdempotencyKey,
        DateTime CreatedAt
    );
}