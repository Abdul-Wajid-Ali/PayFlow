namespace PayFlow.Domain.Events
{
    public record UserRegisteredEvent(Guid UserId, Guid WalletId, DateTime CreatedAt);
}