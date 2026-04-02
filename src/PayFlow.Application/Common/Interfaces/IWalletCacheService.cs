using PayFlow.Application.Common.Models;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IWalletCacheService
    {
        Task<WalletCacheResult?> TryGetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

        Task SetBalanceAsync(WalletCacheResult result, Guid userId, CancellationToken cancellationToken = default);
    }
}