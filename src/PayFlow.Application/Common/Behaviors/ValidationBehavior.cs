using FluentValidation;
using PayFlow.Application.Common.CQRS;
using ValidationException = PayFlow.Application.Common.Exceptions.ValidationException;

namespace PayFlow.Application.Common.Behaviors
{
    // Pipeline behavior for validating requests using FluentValidation
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> HandleAsync(
            TRequest request,
            Func<Task<TResponse>> next,
            CancellationToken cancellationToken = default)
        {
            // If there are no validators, proceed to the next behavior or handler
            if (!_validators.Any())
                return await next();

            // Create a validation context for the request
            var context = new ValidationContext<TRequest>(request);

            // Validate the request using all validators and collect any failures
            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            // If there are any validation failures, throw a ValidationException with the details
            if (failures.Any())
                throw new ValidationException(failures);

            return await next();
        }
    }
}