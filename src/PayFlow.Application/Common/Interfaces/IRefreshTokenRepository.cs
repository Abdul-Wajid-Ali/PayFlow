using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task RevokeAllByUserIdAsync(Guid userId, string reason, DateTime revokedAt, CancellationToken cancellationToken = default);
    }
}