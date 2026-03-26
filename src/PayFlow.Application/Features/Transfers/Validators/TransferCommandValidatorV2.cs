using FluentValidation;
using PayFlow.Application.Features.Transfers.Commands;

namespace PayFlow.Application.Features.Transfers.Validators
{
    public class TransferCommandValidatorV2 : AbstractValidator<TransferCommandV2>
    {
        public TransferCommandValidatorV2()
        {
            //1: The receiver user ID must not be empty.
            RuleFor(x => x.ReceiverUserId)
                .NotEmpty().WithMessage("Receiver is required.");

            //2: The sender user ID must not be empty.
            RuleFor(x => x.SenderUserId)
                .NotEmpty().WithMessage("Sender is required.");

            //3: The amount must be greater than zero.
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            //4: The currency must not be empty, must be a valid 3-letter ISO code, and must be uppercase.
            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Length(3).WithMessage("Currency must be a valid 3-letter ISO code.")
                .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase letters only e.g. USD, EUR.");

            //5: The idempotency key must not be empty and must not exceed 255 characters.
            RuleFor(x => x.IdempotencyKey)
                .NotEmpty().WithMessage("Idempotency Key is required.")
                .MaximumLength(255).WithMessage("Idempotency Key must not exceed 255 characters.");
        }
    }
}