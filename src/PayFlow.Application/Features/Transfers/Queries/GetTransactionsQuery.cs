namespace PayFlow.Application.Features.Transfers.Queries
{
    public record GetTransactionsQuery(Guid UserId, int PageNumber = 1, int PageSize = 20) : IQuery<PagedResult<TransactionResponse>>;
}
