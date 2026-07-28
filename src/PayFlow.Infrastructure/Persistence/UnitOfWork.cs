namespace PayFlow.Infrastructure.Persistence
{
    public class UnitOfWork(PayFlowDbContext dbContext) : IUnitOfWork
    {
        private readonly PayFlowDbContext _dbContext = dbContext;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _dbContext.SaveChangesAsync(cancellationToken);
    }
}