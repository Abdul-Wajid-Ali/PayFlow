namespace PayFlow.API.Controllers.V1
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/wallet")]
    public class WalletController(ISender sender, ICurrentUserService currentUser) : ControllerBase
    {
        private readonly ISender _sender = sender;
        private readonly ICurrentUserService _currentUser = currentUser;

        [HttpGet("balance")]
        [ProducesResponseType(typeof(WalletBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
        {
            // Create GetBalanceQuery with the current user's ID
            var query = new GetBalanceQuery(_currentUser.UserId);

            // Send the query to the handler
            var response = await _sender.Send(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("transactions")]
        [ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Create GetTransactionsQuery with the current user's ID
            var query = new GetTransactionsQuery(_currentUser.UserId, pageNumber, pageSize);

            // Send the query to the handler
            var response = await _sender.Send(query, cancellationToken);

            return Ok(response);
        }
    }
}