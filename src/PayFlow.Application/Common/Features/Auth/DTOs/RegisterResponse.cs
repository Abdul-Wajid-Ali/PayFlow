namespace PayFlow.Application.Common.Features.Auth.DTOs
{
    public record RegisterResponse(Guid UserId, string Email, Guid WalletId);
}