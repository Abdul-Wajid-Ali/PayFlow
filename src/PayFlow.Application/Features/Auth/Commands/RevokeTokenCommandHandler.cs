using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using System.Net;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class RevokeTokenCommandHandler : ICommandHandler<RevokeTokenCommand, bool>
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<RevokeTokenCommandHandler> _logger;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public RevokeTokenCommandHandler(
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<RevokeTokenCommandHandler> logger)
        {
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(RevokeTokenCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh token revocation request received.");

            // 1: Hash incoming refresh token to match stored token
            var refreshTokenHash = _jwtService.HashRefreshToken(command.RefreshToken);

            // 2: Retrieve refresh token from database and throw BusinessRuleException if not found
            var existingRefreshToken = await _refreshTokenRepository.GetByHashAsync(refreshTokenHash, cancellationToken);
            if (existingRefreshToken == null)
            {
                _logger.LogWarning("Token revocation failed: Token not found.");

                throw new BusinessRuleException(
                    title: "Invalid refresh token.",
                    detail: "Refresh token is invalid.",
                    statusCode: (int)HttpStatusCode.Unauthorized);
            }

            // 3: Check if token is already revoked and return success (idempotent behavior)
            if (existingRefreshToken.IsRevoked)
            {
                _logger.LogInformation("Token already revoked for UserId {UserId}.", existingRefreshToken.UserId);

                return true;
            }

            // 4: Revoke token with reason and current timestamp
            existingRefreshToken.Revoke("User logout", _dateTimeProvider.UtcNow);

            // 5: Persist revocation in database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token successfully revoked for UserId {UserId}.", existingRefreshToken.UserId);

            // 6: Return success response
            return true;
        }
    }
}