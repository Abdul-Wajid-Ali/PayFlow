using PayFlow.Application.Common.Interfaces;

namespace PayFlow.Infrastructure.Services
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}