namespace PayFlow.Application.Features.Transfers.Commands
{
    public record TransferCommand(
        Guid SenderUserId,
        Guid ReceiverUserId,
        decimal Amount,
        string Currency,
        string IdempotencyKey
    ) : ICommand<TransferResponse>;
}
