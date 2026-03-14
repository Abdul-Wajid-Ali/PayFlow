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
        private readonly IValidator<RegisterCommand> _registerValidator;
        private readonly ICommandHandler<RegisterCommand, RegisterResponse> _registerHandler;

        public AuthController(IValidator<RegisterCommand> registerValidator,
            ICommandHandler<RegisterCommand, RegisterResponse> registerHandler)
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
                foreach (var error in validationResult.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                return ValidationProblem(ModelState);
            }

            //Handle the command
            var response = await _registerHandler.HandleAsync(command, cancellationToken);

            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }
    }
}