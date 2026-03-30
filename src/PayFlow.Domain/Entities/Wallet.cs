using PayFlow.Domain.Exceptions;

namespace PayFlow.Domain.Entities
{
    public class Wallet
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string Currency { get; private set; } = string.Empty;

        public decimal Balance { get; private set; }

        // Navigation property to the owning user
        public User? User { get; private set; }

        //Navigation collections for transactions where this wallet is sender
        public ICollection<Transaction> OutgoingTransactions { get; private set; } = [];

        // Navigation collections for transactions where this wallet is receiver
        public ICollection<Transaction> IncomingTransactions { get; private set; } = [];

        // Private constructor to enforce use of factory method
        private Wallet()
        { }

        // Factory method to create a new wallet for a user
        public static Wallet Create(Guid userId, string currency = "USD")
         => new()
         {
             Id = Guid.NewGuid(),
             UserId = userId,
             Currency = currency,
             Balance = 0.00m
         };

        // Factory method overload to inittialize existing wallet
        public static Wallet Create(Guid walletId, Guid userId, string currency = "USD", decimal balance = 0.00m)
         => new()
         {
             Id = walletId,
             UserId = userId,
             Currency = currency,
             Balance = balance
         };

        //Domain method - deduct amount from balance
        public void Debit(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidTransferException("Debit amount must be greater than zero.");

            if (Balance < amount)
                throw new InsufficientBalanceException($"Insufficient balance. Available: {Balance} {Currency}, Requested: {amount}.");

            Balance -= amount;
        }

        //Domain method - add amount to balance
        public void Credit(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidTransferException("Credit amount must be greater than zero.");

            Balance += amount;
        }
    }
}