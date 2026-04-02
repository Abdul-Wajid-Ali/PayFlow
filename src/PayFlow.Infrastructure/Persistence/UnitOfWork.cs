using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Interfaces;

namespace PayFlow.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PayFlowDbContext _dbContext;

        public UnitOfWork(PayFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _dbContext.SaveChangesAsync(cancellationToken);
    }
}