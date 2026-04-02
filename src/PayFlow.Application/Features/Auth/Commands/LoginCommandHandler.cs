using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Interfaces;
using System.Net;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly IUserRepository _userRepository;
        private readonly IDateTimeProvider _dateProvider;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LoginCommandHandler(
            IJwtService jwtService,
            IPasswordService passwordService,
            IUserRepository userRepository,
            IDateTimeProvider dateProvider,
            ILogger<LoginCommandHandler> logger,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _jwtService = jwtService;
            _passwordService = passwordService;
            _userRepository = userRepository;
            _dateProvider = dateProvider;
            _logger = logger;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
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

            //5. Genrate and Hash refresh token before storing in DB
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenHash = _jwtService.HashRefreshToken(refreshToken);

            //6. Creates a refresh token entity with metadata and expiration
            var refreshTokenEntity = RefreshToken.Create(
                    userId: user.Id,
                    tokenHash: refreshTokenHash,
                    createdAt: _dateProvider.UtcNow,
                    expiresAt: _dateProvider.UtcNow.AddDays(_jwtService.GetRefreshTokenExpiryInDays())
                );

            //7. Persists the refresh token entity and commit transaction
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Login successful for UserId {UserId}, Email {Email}", user.Id, command.Email);

            return new LoginResponse(
                UserId: user.Id,
                Email: user.Email,
                AccessToken: jwtToken.Value,
                RefreshToken: refreshToken,
                ExpiresAt: jwtToken.ExpiresAt
            );
        }
    }
}