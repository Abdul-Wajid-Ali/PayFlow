using FluentValidation.Results;

namespace PayFlow.Application.Common.Exceptions
{
    // Custom exception for handling validation errors in the PayFlow application
    public class PayFlowValidationException : Exception
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        // Constructor that takes a collection of validation failures and organizes them into a dictionary
        public PayFlowValidationException(IEnumerable<ValidationFailure> failures)
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