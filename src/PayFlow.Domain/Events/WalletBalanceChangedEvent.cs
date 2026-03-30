namespace PayFlow.Domain.Events
{
    public record WalletBalanceChangedEvent(
        Guid WalletId,
        Guid UserId,
        decimal NewBalance,
        string Currency,
        DateTime UpdatedAt
    );
}