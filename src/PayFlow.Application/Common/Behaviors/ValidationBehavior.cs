namespace PayFlow.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            //1: Skip validation if no validators exist
            if (!_validators.Any()) return await next(cancellationToken);

            //2: Create validation context for the request
            var context = new ValidationContext<TRequest>(request);

            //3: Run all validators in parallel
            var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            //4: Aggregate all validation failures
            var failures = results.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            //5: Stop pipeline if any validation fails
            if (failures.Count != 0) throw new ValidationException(failures);

            //6: Continue to next behavior or handler
            return await next(cancellationToken);
        }
    }
}
