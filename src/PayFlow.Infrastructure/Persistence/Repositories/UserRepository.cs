using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly PayFlowDbContext _dbContext;

        public UserRepository(PayFlowDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
            => await _dbContext.Users.AddAsync(user, cancellationToken);

        public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
         => await _dbContext.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}