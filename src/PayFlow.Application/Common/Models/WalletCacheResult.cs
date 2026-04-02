namespace PayFlow.Application.Common.Models
{
    public record WalletCacheResult(
        Guid WalletId,
        Guid UserId,
        string Currency,
        decimal Balance
    );
}