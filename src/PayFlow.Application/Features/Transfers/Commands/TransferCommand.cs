using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.Application.Features.Transfers.Commands
{
    public record TransferCommand(
        Guid SenderUserId,
        Guid RecieverUserId,
        decimal Amount,
        string Currency,
        string IdempotencyKey
    ) : ICommand<TransferResponse>;
}