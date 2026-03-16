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

        public LoginCommandHandler(IJwtService jwtService, IPasswordService passwordService, IUserRepository userRepository)
        {
            _jwtService = jwtService;
            _passwordService = passwordService;
            _userRepository = userRepository;
        }

        public async Task<LoginResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            // 1: Retrieve user by email and throw BusinessRuleException if not found
            var user = await _userRepository.GetByEmailAsync(command.Email);
                if(user == null)
                  throw new BusinessRuleException(
                    title: "Invalid credentials.",
                    detail: "Email or password is incorrect.",
                    statusCode: (int)HttpStatusCode.Unauthorized);

            // 2: Check if user is suspended then throw BusinessRuleException if so
            if (user.Status == UserStatus.Suspended)
                throw new BusinessRuleException(
                    title: "Account suspended.",
                    detail: "Your account has been suspended. Please contact support.",
                    statusCode: (int)HttpStatusCode.Forbidden);

            // 3. Verify password and throw BusinessRuleException if invalid
            var isValidPassword = _passwordService.Verify(command.Password, user.PasswordHash, user.PasswordSalt);
            if (!isValidPassword)
                throw new BusinessRuleException(
                    title: "Invalid credentials.",
                    detail: "Email or password is incorrect.",
                    statusCode: (int)HttpStatusCode.Unauthorized);

            // 4. Generate JWT
            var jwtToken = _jwtService.GenerateToken(user);

            return new LoginResponse(
                UserId: user.Id,
                Email: user.Email,
                Token: jwtToken.Value,
                ExpiresAt: jwtToken.ExpiresAt
            );
        }
    }
}