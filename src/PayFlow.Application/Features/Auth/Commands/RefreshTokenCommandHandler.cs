using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using System.Net;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;
        private readonly IJwtService _jwtService;

        public RefreshTokenCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<RefreshTokenCommandHandler> logger,
            IJwtService jwtService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
            _jwtService = jwtService;
        }

        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh token request received.");

            // 1: Get current UTC time for auditing
            var now = _dateTimeProvider.UtcNow;

            // 2: Hash the incoming refresh token to match stored hashed token in DB
            var currentTokenHash = _jwtService.HashRefreshToken(command.RefreshToken);

            // 3: Retrieve the refresh token and throw BusinessRuleException if not found
            var existingToken = await _refreshTokenRepository.GetByHashAsync(currentTokenHash, cancellationToken);
            if (existingToken == null)
            {
                _logger.LogWarning("Refresh failed: token not found.");

                throw new BusinessRuleException(
                    title: "Invalid refresh token.",
                    detail: "Refresh token not found.",
                    statusCode: (int)HttpStatusCode.Unauthorized);
            }

            _logger.LogInformation("Refresh token found for UserId {UserId}", existingToken.UserId);

            // 4: Detect refresh token reuse (token already revoked) and revoke all active sessions for security
            if (existingToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for UserId {UserId}. Revoking all sessions.",
                    existingToken.UserId);

                await _refreshTokenRepository.RevokeAllByUserIdAsync(
                    existingToken.UserId,
                    "Refresh token reuse detected",
                    now,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new BusinessRuleException(
                    title: "Refresh token reuse detected.",
                    detail: "Session has been revoked. Please login again.",
                    statusCode: (int)HttpStatusCode.Unauthorized);
            }

            // 5: Validate token expiration and revoke it if expired
            if (existingToken.IsExpired(now))
            {
                _logger.LogWarning(
                    "Refresh token expired for UserId {UserId}",
                    existingToken.UserId);

                existingToken.Revoke("Expired", now);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new BusinessRuleException(
                    title: "Refresh token expired.",
                    detail: "Please login again.",
                    statusCode: (int)HttpStatusCode.Unauthorized);
            }

            // 6: Retrieve associated user and validate account status
            var tokenUser = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
            if (tokenUser == null)
            {
                _logger.LogWarning(
                    "Refresh failed: user not found for UserId {UserId}",
                    existingToken.UserId);

                throw new BusinessRuleException(
                    title: "Access denied.",
                    detail: "User not found.",
                    statusCode: (int)HttpStatusCode.Forbidden);
            }

            if (tokenUser.Status == UserStatus.Suspended)
            {
                _logger.LogWarning(
                    "Refresh failed: suspended user {UserId}",
                    tokenUser.Id);

                throw new BusinessRuleException(
                    title: "Access denied.",
                    detail: "User account is suspended.",
                    statusCode: (int)HttpStatusCode.Forbidden);
            }

            // 7: Generate new access token for authenticated session continuation
            var accessToken = _jwtService.GenerateToken(tokenUser);

            // 8: Generate and hash new refresh token (rotation)
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var hashRefreshToken = _jwtService.HashRefreshToken(newRefreshToken);

            // 9: Rotate the existing token (mark as revoked and link replacement)
            existingToken.Rotate(hashRefreshToken, now);

            // 10: Create replacement refresh token
            var replacementTokenEntity = RefreshToken.Create(
                tokenUser.Id,
                hashRefreshToken,
                now,
                now.AddDays(_jwtService.GetRefreshTokenExpiryInDays()));

            // 11: Persist new refresh token and commit transaction
            await _refreshTokenRepository.AddAsync(replacementTokenEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refresh successful for UserId {UserId}. New tokens issued.",
                tokenUser.Id);

            // 12: Return new access token and refresh token to client
            return new(
                AccessToken: accessToken.Value,
                RefreshToken: newRefreshToken,
                ExpiresAt: accessToken.ExpiresAt);
        }
    }
}