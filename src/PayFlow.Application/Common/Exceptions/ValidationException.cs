namespace PayFlow.Application.Common.Exceptions
{
    // Custom exception to represent validation errors
    public class ValidationException(IEnumerable<ValidationFailure> failures) : Exception("One or more validation errors occurred.")
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; } = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray()
            );
    }
}
