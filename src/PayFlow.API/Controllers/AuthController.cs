using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Features.Auth.Commands;
using PayFlow.Application.Common.Features.Auth.DTOs;

namespace PayFlow.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            // Create the command
            var command = new RegisterCommand(Email: request.Email, Password: request.Password);

            // Send the command to the handler
            var response = await _sender.SendAsync(command, cancellationToken);

            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Login([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            // Create the command
            var command = new RegisterCommand(Email: request.Email, Password: request.Password);

            // Send the command to the handler
            var response = await _sender.SendAsync(command, cancellationToken);

            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }
    }
}