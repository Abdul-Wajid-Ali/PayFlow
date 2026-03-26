using FluentValidation;
using PayFlow.Application.Features.Auth.Commands;

namespace PayFlow.Application.Features.Auth.Validators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            //1: The email must not be empty and must be in a valid email format.
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            //2: The password must not be empty.
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}