using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.DTOs;
using System.Net;

namespace PayFlow.Application.Features.Transfers.Queries
{
    public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, IReadOnlyList<TransactionResponse>>
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<GetTransactionsQueryHandler> _logger;

        public GetTransactionsQueryHandler(
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository,
            ILogger<GetTransactionsQueryHandler> logger)
        {
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TransactionResponse>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Transaction retrieval initiated for UserId {UserId}",
                query.UserId);

            //1: Validate wallet existence and throw BusinessRuleException if not found
            var wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (wallet == null)
            {
                _logger.LogWarning(
                    "Transaction retrieval failed: wallet not found for UserId {UserId}",
                    query.UserId);

                throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: "No wallet is associated with this account.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            //2: Retrieve transactions for the wallet
            var transactions = await _transactionRepository.GetByWalletIdAsync(wallet.Id, cancellationToken);

            _logger.LogInformation(
                "Retrieved {TransactionCount} transactions for WalletId {WalletId} (UserId {UserId})",
                transactions.Count,
                wallet.Id,
                query.UserId);

            //3: Map transactions to response DTOs, including direction (incoming/outgoing)
            var response = transactions.Select(t => new TransactionResponse
            (
                TransactionId: t.Id,
                FromWalletId: t.FromWalletId,
                ToWalletId: t.ToWalletId,
                Amount: t.Amount,
                Currency: t.Currency,
                Status: t.Status,
                CreatedAt: t.CreatedAt,
                Direction: t.FromWalletId == wallet.Id ? "Outgoing" : "Incoming"
            )).ToList();

            _logger.LogInformation(
                "Transaction retrieval completed for WalletId {WalletId}",
                wallet.Id);

            return response;
        }
    }
}