using PayFlow.Domain.Enums;

namespace PayFlow.Application.Features.Transfers.DTOs
{
    //DTO for receiving Transfer Request
    public record TransferRequest(Guid ReceiverUserId, decimal Amount, string Currency);

    //DTO for returning Transfer Response
    public record TransferResponse(
        Guid TransactionId, 
        Guid FromWalletId, 
        Guid ToWalletId, 
        decimal Amount, 
        string Currency, 
        TransactionStatus Status, 
        DateTime CreatedAt);

    //DTO for returning Transfer Response with Direction
    public record TransactionResponse(
        Guid TransactionId,
        Guid FromWalletId,
        Guid ToWalletId,
        decimal Amount,
        string Currency,
        TransactionStatus Status,
        DateTime CreatedAt,
        string Direction);
}