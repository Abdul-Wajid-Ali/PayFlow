using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.DTOs;
using System.Net;

namespace PayFlow.Application.Features.Transfers.Queries
{
    public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, IReadOnlyList<TransactionResponse>>
    {
        private IWalletRepository _walletRepository;
        private ITransactionRepository _transactionRepository;

        public GetTransactionsQueryHandler(IWalletRepository walletRepository, ITransactionRepository transactionRepository)
        {
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(GetTransactionsQuery query, CancellationToken cancellationToken = default)
        {
            //1: Validate wallet existence and throw BusinessRuleException if not found
            var wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (wallet == null)
                throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: "No wallet is associated with this account.",
                    statusCode: (int)HttpStatusCode.NotFound);

            //2: Retrieve transactions for the wallet and map to TransactionResponse DTOs
            var transactions = await _transactionRepository.GetByWalletIdAsync(wallet.Id, cancellationToken);

            //3: Map transactions to TransactionResponse, determining direction based on wallet association
            return transactions.Select(t => new TransactionResponse
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
        }
    }
}