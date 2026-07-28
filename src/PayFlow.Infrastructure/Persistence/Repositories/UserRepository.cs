namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class UserRepository(PayFlowDbContext dbContext) : IUserRepository
    {
        private readonly PayFlowDbContext _dbContext = dbContext;

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
            => await _dbContext.Users.AddAsync(user, cancellationToken);

        public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
         => await _dbContext.Users.AnyAsync(u => u.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase), cancellationToken);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase), cancellationToken);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}