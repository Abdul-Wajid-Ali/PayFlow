using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using System.Net;

namespace PayFlow.Application.Features.Wallet.Queries
{
    public class GetBalanceQueryHandler : IQueryHandler<GetBalanceQuery, WalletBalanceResponse>
    {
        private readonly IWalletRepository _walletRepository;

        public GetBalanceQueryHandler(IWalletRepository walletRepository)
            => _walletRepository = walletRepository;

        public async Task<WalletBalanceResponse> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
        {
            // 1: Retrieve user by email and throw BusinessRuleException if not found
            var userWallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken)
                ?? throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: $"No wallet found for user with ID {query.UserId}.",
                    statusCode: (int)HttpStatusCode.NotFound);

            //2: Map Wallet entity to WalletBalanceResponse DTO
            return new WalletBalanceResponse
            (
                WalletId: userWallet.Id,
                UserId: userWallet.UserId,
                Balance: userWallet.Balance,
                Currency: userWallet.Currency
            );
        }
    }
}