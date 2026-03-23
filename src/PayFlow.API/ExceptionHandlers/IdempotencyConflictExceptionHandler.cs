using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.Exceptions;

namespace PayFlow.API.ExceptionHandlers
{
    public class IdempotencyConflictExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<IdempotencyConflictExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public IdempotencyConflictExceptionHandler(ILogger<IdempotencyConflictExceptionHandler> logger
            , IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            //1: Check if the exception is of type IdempotencyConflictException
            if (exception is not IdempotencyConflictException idempotencyConflictException)
                return false;

            // 2: Create a ProblemDetails object with the details from the BusinessRuleException
            var problemDetails = new ProblemDetails
            {
                Type = idempotencyConflictException.GetType().Name,
                Title = "Idempotency Conflict",
                Detail = idempotencyConflictException.Message,
                Status = StatusCodes.Status409Conflict,
                Instance = httpContext.Request.Path
            };

            // 3: Set the response status code and content type
            _logger.LogWarning("Idempotency conflict for {Method} {Path}. Detail: {Detail}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            idempotencyConflictException.Message);

            // 4: Set the response status code and content type
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            // 5: Create a ProblemDetails response using the IProblemDetailsService
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = idempotencyConflictException,
                ProblemDetails = problemDetails
            });
        }
    }
}