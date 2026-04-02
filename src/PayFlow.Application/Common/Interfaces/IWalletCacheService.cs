using PayFlow.Application.Features.Wallet.DTOs;

namespace PayFlow.Application.Common.Interfaces;

public interface IWalletCacheService
{
    Task<WalletBalanceResponse?> TryGetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetBalanceAsync(WalletBalanceResponse balance, Guid userId, CancellationToken cancellationToken = default);
}
