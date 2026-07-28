namespace PayFlow.Application.Features.Wallet.Queries
{
    public class GetBalanceQueryHandler(
        IWalletRepository walletRepository,
        ILogger<GetBalanceQueryHandler> logger) : IQueryHandler<GetBalanceQuery, WalletBalanceResponse>
    {
        private readonly IWalletRepository _walletRepository = walletRepository;
        private readonly ILogger<GetBalanceQueryHandler> _logger = logger;

        public async Task<WalletBalanceResponse> Handle(GetBalanceQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Balance retrieval initiated for UserId {UserId}",
                query.UserId);

            //1: Validate wallet existence and throw BusinessRuleException if not found
            var wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (wallet is null)
            {
                _logger.LogWarning(
                    "Balance retrieval failed: wallet not found for UserId {UserId}",
                    query.UserId);

                throw new BusinessRuleException(
                    title: "Wallet not found.",
                    detail: $"No wallet found for user with ID {query.UserId}.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            _logger.LogInformation(
                "Balance retrieved successfully for UserId {UserId}. WalletId {WalletId}, Balance {Balance} {Currency}",
                wallet.UserId,
                wallet.Id,
                wallet.Balance,
                wallet.Currency);

            //2: Return WalletBalanceResponse DTO
            return new WalletBalanceResponse(
                WalletId: wallet.Id,
                UserId: wallet.UserId,
                Balance: wallet.Balance,
                Currency: wallet.Currency
            );
        }
    }
}
