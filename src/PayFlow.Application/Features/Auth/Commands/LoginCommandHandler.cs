using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Domain.Enums;
using System.Net;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IJwtService jwtService,
            IPasswordService passwordService,
            IUserRepository userRepository,
            ILogger<LoginCommandHandler> logger)
        {
            _jwtService = jwtService;
            _passwordService = passwordService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login attempt for email: {Email}", command.Email);

            // 1: Retrieve user by email and throw BusinessRuleException if not found
            var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found for Email {Email}", command.Email);

                throw new BusinessRuleException(
                  title: "Invalid credentials.",
                  detail: "Email or password is incorrect.",
                  statusCode: (int)HttpStatusCode.Unauthorized);
            }

            // 2: Check if user is suspended then throw BusinessRuleException if so
            if (user.Status == UserStatus.Suspended)
            {
                _logger.LogWarning("Login failed: account suspended for UserId {UserId}, Email {Email}", user.Id, command.Email);

                throw new BusinessRuleException(
                    title: "Account suspended.",
                    detail: "Your account has been suspended. Please contact support.",
                    statusCode: (int)HttpStatusCode.Forbidden);
            }

            // 3. Verify password and throw BusinessRuleException if invalid
            var isValidPassword = _passwordService.Verify(command.Password, user.PasswordHash, user.PasswordSalt);
            if (!isValidPassword)
            {
                _logger.LogWarning("Login failed: invalid password for UserId {UserId}, Email {Email}", user.Id, command.Email);

                throw new BusinessRuleException(
                    title: "Invalid credentials.",
                    detail: "Email or password is incorrect.",
                    statusCode: (int)HttpStatusCode.Unauthorized);
            }

            // 4. Generate JWT
            var jwtToken = _jwtService.GenerateToken(user);

            _logger.LogInformation("Login successful for UserId {UserId}, Email {Email}", user.Id, command.Email);

            return new LoginResponse(
                UserId: user.Id,
                Email: user.Email,
                Token: jwtToken.Value,
                ExpiresAt: jwtToken.ExpiresAt
            );
        }
    }
}