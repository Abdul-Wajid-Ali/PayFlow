namespace PayFlow.Application.Features.Transfers.Commands
{
    public record TransferCommandV2(
        Guid SenderUserId,
        Guid ReceiverUserId,
        decimal Amount,
        string Currency,
        string IdempotencyKey
    ) : ICommand<TransferAcceptedResponse>;
}
