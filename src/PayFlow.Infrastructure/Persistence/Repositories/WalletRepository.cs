using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly PayFlowDbContext _dbContext;

        public WalletRepository(PayFlowDbContext dbContext)
            => _dbContext = dbContext;

        public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
         => await _dbContext.Wallets.AddAsync(wallet, cancellationToken);

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
         => await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        public async Task<WalletBalanceResponse?> GetBalanceDtoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _dbContext.Wallets
            .Where(w => w.UserId == userId)
            .Select(w => new WalletBalanceResponse(
                WalletId: w.Id,
                UserId: w.UserId,
                Balance: w.Balance,
                Currency: w.Currency))
            .FirstOrDefaultAsync(cancellationToken);
    }
}