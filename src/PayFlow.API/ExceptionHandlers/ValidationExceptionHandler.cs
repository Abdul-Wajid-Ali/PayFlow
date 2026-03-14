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
            //1: Check if the exception is a ValidationException.
            if (exception is not ValidationException validationException)
                return false;

            //2: Create a ValidationProblemDetails instance and populate it with details from the exception.
            var problemDetails = new ValidationProblemDetails()
            {
                Type = validationException.GetType().Name,
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Instance = httpContext.Request.Path
            };

            // Manually copy errors — avoids ModelStateDictionary conversion issue
            foreach (var (key, messages) in validationException.Errors)
                problemDetails.Errors[key] = messages;

            //3: Log the exception details using the ILogger.
            _logger.LogError(exception
                , problemDetails.Title
                , problemDetails.Instance
                , problemDetails.Errors);

            //4: Set the HTTP response status code to 400 Bad Request.
            httpContext.Response.StatusCode = (int)problemDetails.Status;

            //5: Use the IProblemDetailsService to write the ProblemDetails response back to the client.
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = validationException,
                ProblemDetails = problemDetails
            });
        }
    }
}