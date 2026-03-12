namespace PayFlow.Domain.Entities
{
    public class Wallet
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Currency { get; set; }

        public decimal Balance { get; set; }
    }
}