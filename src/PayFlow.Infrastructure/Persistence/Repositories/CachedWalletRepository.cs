namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class CachedWalletRepository(
        IWalletRepository innerRepository,
        IWalletCacheService walletCacheService) : IWalletRepository
    {
        private readonly IWalletRepository _innerRepository = innerRepository;
        private readonly IWalletCacheService _walletCacheService = walletCacheService;

        public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(wallet, cancellationToken);

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 1: Try cache first
            var cacheResult = await _walletCacheService.TryGetBalanceAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            if (cacheResult is not null)
                return Wallet.Create(
                    walletId: cacheResult.WalletId,
                    userId: cacheResult.UserId,
                    currency: cacheResult.Currency,
                    balance: cacheResult.Balance
                );

            // 2: Fallback to database (source of truth)
            var wallet = await _innerRepository.GetByUserIdAsync(userId: userId, cancellationToken: cancellationToken);

            return wallet is not null ? wallet : null;
        }

        public Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(walletId, cancellationToken);
    }
}