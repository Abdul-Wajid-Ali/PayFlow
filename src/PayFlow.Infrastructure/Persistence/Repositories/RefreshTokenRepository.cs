using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly PayFlowDbContext _dbContext;

        public RefreshTokenRepository(PayFlowDbContext dbContext)
            => _dbContext = dbContext;

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
            => await _dbContext.AddAsync(refreshToken, cancellationToken);

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
         => await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        public async Task RevokeAllByUserIdAsync(Guid userId, string reason, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            //1: Get all active refresh tokens for the user
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            //2: Revoke each active token
            foreach (var token in activeTokens)
                token.Revoke(reason, revokedAt);
        }
    }
}