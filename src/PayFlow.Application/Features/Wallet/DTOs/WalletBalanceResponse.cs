namespace PayFlow.Application.Features.Wallet.DTOs
{
    public record WalletBalanceResponse(
        Guid WalletId,
        Guid UserId,
        decimal Balance,
        string Currency
    );
}