using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Features.Auth.Commands;
using PayFlow.Application.Features.Auth.DTOs;

namespace PayFlow.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
            => _sender = sender;

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            // Create RegisterCommand from the request
            var command = new RegisterCommand(Email: request.Email, Password: request.Password);

            // Send the command to the handler
            var response = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            // Create LoginCommand from the request
            var command = new LoginCommand(Email: request.Email, Password: request.Password);

            // Send the command to the handler
            var response = await _sender.Send(command, cancellationToken);

            return Ok(response);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            // Create RefreshTokenCommand from the request
            var command = new RefreshTokenCommand(request.RefreshToken);

            // Send the command to the handler
            var respone = await _sender.Send(command,cancellationToken);

            return Ok(respone);
        }

        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
        {
            // Create RevokeTokenCommand from the request
            var command = new RevokeTokenCommand(request.RefreshToken);

            // Send the command to the handler
            var response = await _sender.Send(command, cancellationToken);

            return Ok(response);
        }
    }
}