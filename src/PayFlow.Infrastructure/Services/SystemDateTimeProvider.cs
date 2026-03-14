using PayFlow.Application.Common.Interfaces;

namespace PayFlow.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
