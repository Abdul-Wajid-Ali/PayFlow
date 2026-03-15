namespace PayFlow.Application.Common.Interfaces
{
    public class ICurrentUserService
    {
        public Guid UserId { get; }

        public string Email { get; }

        public bool IsAuthenticated { get; }
    }
}