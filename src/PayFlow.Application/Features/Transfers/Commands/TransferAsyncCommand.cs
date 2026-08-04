namespace PayFlow.Application.Features.Transfers.Commands
{
    public record TransferAsyncCommand(
        Guid SenderUserId,
        Guid ReceiverUserId,
        decimal Amount,
        string Currency,
        string IdempotencyKey
    ) : ICommand<TransferAcceptedResponse>;
}
