using FluentValidation;
using PayFlow.Application.Features.Transfers.Queries;

namespace PayFlow.Application.Features.Transfers.Validators
{
    public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
    {
        public GetTransactionsQueryValidator()
        {
            // 1: The page number must be greater than 0.
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            // 2: The page number must be between 1 and 100 range.
            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}