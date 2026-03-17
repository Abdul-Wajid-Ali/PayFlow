using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GetBalanceQueryHandler> _logger;

        public GetBalanceQueryHandler(
            IWalletRepository walletRepository,
            ILogger<GetBalanceQueryHandler> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<WalletBalanceResponse> Handle(GetBalanceQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Balance retrieval initiated for UserId {UserId}",
                query.UserId);

            //1: Validate wallet existence and throw BusinessRuleException if not found
            var userWallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (userWallet is null)
            {
                _logger.LogWarning(
                    "Balance retrieval failed: wallet not found for UserId {UserId}",
                    query.UserId);

                throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: $"No wallet found for user with ID {query.UserId}.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            _logger.LogInformation(
                "Balance retrieved successfully for UserId {UserId}. WalletId {WalletId}, Balance {Balance} {Currency}",
                userWallet.UserId,
                userWallet.Id,
                userWallet.Balance,
                userWallet.Currency);

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