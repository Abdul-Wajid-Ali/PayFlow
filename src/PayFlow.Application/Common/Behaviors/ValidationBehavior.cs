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
            // 1: Skip validation entirely if no validators are registered for this request type
            if (!_validators.Any())
                return await next();

            // 2: Build a validation context wrapping the incoming request data
            var context = new ValidationContext<TRequest>(request);

            // 3: Run every registered validator and flatten all rule failures into one list
            var validationTasks = _validators
                .Select(v => v.ValidateAsync(context, cancellationToken));

            var validationResults = await Task.WhenAll(validationTasks);

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            // 4: Abort the pipeline and surface all validation errors if any rule failed
            if (failures.Any())
                throw new ValidationException(failures);

            // 5: All rules passed — hand control to the next behavior or the command handler
            return await next();
        }
    }
}