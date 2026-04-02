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
        private readonly IWalletCacheService _walletCacheService;
        private readonly ILogger<GetBalanceQueryHandler> _logger;

        public GetBalanceQueryHandler(
            IWalletRepository walletRepository,
            IWalletCacheService walletCacheService,
            ILogger<GetBalanceQueryHandler> logger)
        {
            _walletRepository = walletRepository;
            _walletCacheService = walletCacheService;
            _logger = logger;
        }

        public async Task<WalletBalanceResponse> Handle(GetBalanceQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Balance retrieval initiated for UserId {UserId}",
                query.UserId);

            //1: Try cache first
            var cached = await _walletCacheService.TryGetBalanceAsync(query.UserId, cancellationToken);
            if (cached is not null)
            {
                _logger.LogInformation(
                    "Balance retrieved from cache for UserId {UserId}. WalletId {WalletId}, Balance {Balance} {Currency}",
                    cached.UserId, cached.WalletId, cached.Balance, cached.Currency);
                return cached;
            }

            //2: Cache miss — fetch wallet entity from database
            var wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (wallet is null)
            {
                _logger.LogWarning(
                    "Balance retrieval failed: wallet not found for UserId {UserId}",
                    query.UserId);

                throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: $"No wallet found for user with ID {query.UserId}.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            //3: Map entity to DTO
            var balanceDto = new WalletBalanceResponse(
                WalletId: wallet.Id,
                UserId: wallet.UserId,
                Balance: wallet.Balance,
                Currency: wallet.Currency);

            //4: Populate cache for future requests
            await _walletCacheService.SetBalanceAsync(balanceDto, query.UserId, cancellationToken);

            _logger.LogInformation(
                "Balance retrieved from DB for UserId {UserId}. WalletId {WalletId}, Balance {Balance} {Currency}",
                balanceDto.UserId, balanceDto.WalletId, balanceDto.Balance, balanceDto.Currency);

            return balanceDto;
        }
    }
}
