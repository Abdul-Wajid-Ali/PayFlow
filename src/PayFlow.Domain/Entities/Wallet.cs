namespace PayFlow.Domain.Entities
{
    public class Wallet
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string Currency { get; private set; } = string.Empty;

        public decimal Balance { get; private set; }

        // Navigation property to the owning user
        public User? User { get; set; }

        //Navigation collections for transactions where this wallet is sender
        public ICollection<Transaction> OutgoingTransactions { get; private set; } = new List<Transaction>();

        // Navigation collections for transactions where this wallet is receiver
        public ICollection<Transaction> IncomingTransactions { get; private set; } = new List<Transaction>();

        // Private constructor to enforce use of factory method
        private Wallet()
        { }

        // Factory method to create a new wallet for a user
        public static Wallet Create(Guid userId, string currency = "USD")
        {
            return new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Currency = currency,
                Balance = 0.00m
            };
        }
    }
}