using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.Features.Auth.Commands;
using PayFlow.Application.Common.Features.Auth.DTOs;

namespace PayFlow.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RegisterCommandHandler _registerHandler;
        private readonly IValidator<RegisterCommand> _registerValidator;

        public AuthController(RegisterCommandHandler registerHandler, IValidator<RegisterCommand> registerValidator)
        {
            _registerHandler = registerHandler;
            _registerValidator = registerValidator;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var command = new RegisterCommand(Email: request.Email, Password: request.Password);

            // Validate the command
            var validationResult = await _registerValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var problemDetails = new ValidationProblemDetails(validationResult.ToDictionary())
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                };

                return BadRequest(problemDetails);
            }

            //Handle the command
            var response = await _registerHandler.HandleAsync(command, cancellationToken);

            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }
    }
}