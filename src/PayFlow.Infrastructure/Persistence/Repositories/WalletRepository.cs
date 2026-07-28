namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class WalletRepository(PayFlowDbContext dbContext) : IWalletRepository
    {
        private readonly PayFlowDbContext _dbContext = dbContext;

        public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
         => await _dbContext.Wallets.AddAsync(wallet, cancellationToken);

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
         => await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        public async Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
        => await _dbContext.Wallets.FindAsync(walletId, cancellationToken);
    }
}
