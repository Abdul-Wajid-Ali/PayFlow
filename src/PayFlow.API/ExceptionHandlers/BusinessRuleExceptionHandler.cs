using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Common.Exceptions;

namespace PayFlow.API.ExceptionHandlers
{
    public class BusinessRuleExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<BusinessRuleExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger
            , IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            //1: Check if the exception is of type BusinessRuleException
            if (exception is not BusinessRuleException businessRuleException)
                return false;

            // 2: Create a ProblemDetails object with the details from the BusinessRuleException
            var problemDetails = new ProblemDetails
            {
                Type = businessRuleException.GetType().Name,
                Title = businessRuleException.Title,
                Detail = businessRuleException.Detail,
                Status = businessRuleException.StatusCode,
                Instance = httpContext.Request.Path
            };

            //3: Log the business rule violation with details
            _logger.LogError(exception
                , problemDetails.Title
                , problemDetails.Instance
                , problemDetails.Detail);

            // 4: Set the response status code and content type
            httpContext.Response.StatusCode = (int)problemDetails.Status;

            // 5: Create a ProblemDetails response using the IProblemDetailsService
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = businessRuleException,
                ProblemDetails = problemDetails
            });
        }
    }
}