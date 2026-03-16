using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.Application.Features.Transfers.Queries
{
    public record GetTransactionsQuery(Guid UserId) : IQuery<IReadOnlyList<TransactionResponse>>;
}