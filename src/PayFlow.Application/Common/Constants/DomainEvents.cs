namespace PayFlow.Application.Common.Constants
{
    public static class DomainEvents
    {
        public const string UserRegistered = "user.registered";

        public const string TransferRequested = "transfer.requested";

        public const string TransferCompleted = "transfer.completed";

        public const string WalletBalanceChanged = "wallet.balance.changed";
    }
}