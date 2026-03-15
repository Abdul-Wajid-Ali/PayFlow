using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Application.Features.Wallet.Queries;

namespace PayFlow.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUserService _currentUser;

        public WalletController(ISender sender, ICurrentUserService currentUser)
        {
            _sender = sender;
            _currentUser = currentUser;
        }

        [HttpGet("balance")]
        [ProducesResponseType(typeof(WalletBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
        {
            // Create GetBalanceQuery with the current user's ID
            var query = new GetBalanceQuery(_currentUser.UserId);

            // Send the query to the handler
            var response = await _sender.QueryAsync(query, cancellationToken);

            return Ok(response);
        }
    }
}