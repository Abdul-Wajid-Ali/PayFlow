using FluentValidation.Results;

namespace PayFlow.Application.Common.Exceptions
{
    // Custom exception to represent validation errors
    public class ValidationException : Exception
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        // Constructor that takes a list of validation failures and organizes them into a dictionary
        public ValidationException(IEnumerable<ValidationFailure> failures)
            : base("One or more validation errors occurred.")
        {
            Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray()
            );
        }
    }
}