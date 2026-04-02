using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);

        Task<WalletBalanceResponse?> GetBalanceDtoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}