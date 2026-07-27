namespace PayFlow.Application.Features.Auth.Validators
{
    public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
    {
        public RevokeTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
    .NotEmpty().WithMessage("Refresh token is required.");
        }
    }
}
