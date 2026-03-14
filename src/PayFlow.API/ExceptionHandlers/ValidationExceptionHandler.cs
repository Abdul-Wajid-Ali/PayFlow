using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.Exceptions;

namespace PayFlow.API.ExceptionHandlers
{
    public class ValidationExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ValidationExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger
            , IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Check if the exception is of type PayFlowValidationException
            if (exception is not PayFlowValidationException validationException)
                return false;

            // Log the validation error with details
            _logger.LogError(exception, "Validation error occurred."
                , httpContext.Request.Path
                , validationException.Errors);

            // Set the response status code and content type
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            // Create a ValidationProblemDetails object to represent the validation errors
            var validationProblemDetails = new ValidationProblemDetails()
            {
                Type = validationException.GetType().Name,
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Instance = httpContext.Request.Path
            };

            // Manually copy errors — avoids ModelStateDictionary conversion issue
            foreach (var (key, messages) in validationException.Errors)
                validationProblemDetails.Errors[key] = messages;

            // Use the ProblemDetailsService to write the response and return the result
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = validationException,
                ProblemDetails = validationProblemDetails
            });
        }
    }
}