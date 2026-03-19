using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Models;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.Application.Features.Transfers.Queries
{
    public record GetTransactionsQuery(Guid UserId, int PageNumber = 1, int PageSize = 20) : IQuery<PagedResult<TransactionResponse>>;
}