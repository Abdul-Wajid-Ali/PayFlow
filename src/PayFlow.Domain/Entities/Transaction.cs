using System.Transactions;

namespace PayFlow.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }

        public Guid FromWalletId { get; set; }
        public Wallet FromWallet { get; set; }

        public Guid ToWalletId { get; set; }
        public Wallet ToWallet { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public TransactionStatus Status { get; set; }

        public string IdempotencyKey { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}