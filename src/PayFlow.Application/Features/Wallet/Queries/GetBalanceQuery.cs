namespace PayFlow.Application.Features.Wallet.Queries
{
    // Query to get the balance of a user's wallet for a given user ID.
    public record GetBalanceQuery(Guid UserId) : IQuery<WalletBalanceResponse>;
}
