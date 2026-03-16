namespace PayFlow.Application.Features.Wallet.DTOs
{
    // DTO for returning Wallet Balance Response
    public record WalletBalanceResponse(
        Guid WalletId,
        Guid UserId,
        decimal Balance,
        string Currency
    );
}