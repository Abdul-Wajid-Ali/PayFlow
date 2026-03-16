using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.Commands;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/transfer")]
    public class TransferController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUserService _currentUser;

        public TransferController(ISender sender, ICurrentUserService currentUser)
        {
            _sender = sender;
            _currentUser = currentUser;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Transfer(
            [FromBody] TransferRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var command = new TransferCommand(
                SenderUserId: _currentUser.UserId,
                RecieverUserId: request.RecieverUserId,
                Amount: request.Amount,
                Currency: request.Currency,
                IdempotencyKey: idempotencyKey
            );

            var response = await _sender.SendAsync(command, cancellationToken);

            return CreatedAtAction(nameof(Transfer), new { id = response.TransactionId }, response);
        }
    }
}