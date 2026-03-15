namespace PayFlow.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        public Guid UserId { get; }

        public string Email { get; }

        public bool IsAuthenticated { get; }
    }
}