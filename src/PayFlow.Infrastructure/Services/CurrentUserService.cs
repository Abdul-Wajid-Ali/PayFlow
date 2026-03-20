using Microsoft.AspNetCore.Http;
using PayFlow.Application.Common.Interfaces;

namespace PayFlow.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor;

        // Read the user ID from the "userId" claim in the JWT token. If the claim is not present or cannot be parsed, return Guid.Empty.
        public Guid UserId
            => Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value, out var id) ? id : Guid.Empty;

        // Read the email from the "email" claim in the JWT token. If the claim is not present, return an empty string.
        public string Email
            => _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value ?? string.Empty;

        // Check if the user is authenticated by checking the IsAuthenticated property of the user's identity.
        // If the HttpContext or User or Identity is null, return false.
        public bool IsAuthenticated
            => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}