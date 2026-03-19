using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.Commands;
using PayFlow.Application.Features.Transfers.DTOs;

namespace PayFlow.API.Controllers.V1
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/transfer")]
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
                ReceiverUserId: request.ReceiverUserId,
                Amount: request.Amount,
                Currency: request.Currency,
                IdempotencyKey: idempotencyKey
            );

            var response = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(Transfer), new { id = response.TransactionId }, response);
        }
    }
}