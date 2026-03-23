using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.Application.Features.Transfers.Commands
{
    public record TransferCommandV2(
        Guid SenderUserId,
        Guid ReceiverUserId,
        decimal Amount,
        string Currency,
        string IdempotencyKey
    ) : ICommand<TransferResponse>;
}