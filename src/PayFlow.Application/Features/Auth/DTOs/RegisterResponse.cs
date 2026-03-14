namespace PayFlow.Application.Features.Auth.DTOs
{
    public record RegisterResponse(Guid UserId, string Email, Guid WalletId);
}
