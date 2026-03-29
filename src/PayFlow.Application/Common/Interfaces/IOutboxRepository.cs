using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, DateTime dateTimeNow, CancellationToken cancellationToken = default);
    }
}