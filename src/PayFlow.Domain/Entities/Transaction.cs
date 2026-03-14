using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }

    public Guid FromWalletId { get; private set; }

    public Guid ToWalletId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public TransactionStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Wallet? FromWallet { get; private set; }
    public Wallet? ToWallet { get; private set; }

    private Transaction() { } // EF Core constructor

    public static Transaction Create(
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount,
        string currency,
        string idempotencyKey)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            FromWalletId = fromWalletId,
            ToWalletId = toWalletId,
            Amount = amount,
            Currency = currency,
            Status = TransactionStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkCompleted() => Status = TransactionStatus.Completed;

    public void MarkFailed() => Status = TransactionStatus.Failed;
}