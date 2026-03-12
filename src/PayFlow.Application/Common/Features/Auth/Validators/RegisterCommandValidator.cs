using FluentValidation;
using PayFlow.Application.Common.Features.Auth.Commands;

namespace PayFlow.Application.Common.Features.Auth.Validators
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        // This validator ensures that the email and password provided in the RegisterCommand meet certain criteria:
        // - The email must not be empty, must be in a valid email format, and must not exceed 255 characters.
        // - The password must not be empty, must be at least 8 characters long, and must not exceed 128 characters.
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
        }
    }
}