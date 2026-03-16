using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Domain.Exceptions;

namespace PayFlow.API.ExceptionHandlers
{
    public class DomainExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<DomainExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger
            , IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            //1: Check if the exception is of type DomainException
            if (exception is not DomainException domainException)
                return false;

            //2: Check the type of domain exception and map it to a status code and title
            var (status, title) = domainException switch
            {
                InsufficientBalanceException => (StatusCodes.Status422UnprocessableEntity, "Insufficient balance."),
                InvalidTransferException => (StatusCodes.Status400BadRequest, "Invalid transfer."),
                _ => (StatusCodes.Status400BadRequest, "Domain Error")
            };

            // 3: Create a ProblemDetails object with the details from the DomainException
            var problemDetails = new ProblemDetails
            {
                Type = domainException.GetType().Name,
                Title = title,
                Detail = domainException.Message,
                Status = status,
                Instance = httpContext.Request.Path
            };

            //4: Log the domain rule violation with details
            _logger.LogWarning(exception
                , problemDetails.Title
                , problemDetails.Instance
                , "Domain rule violated — Title: {Title}, Detail: {Detail}, Path: {Path}");

            // 5: Set the response status code and content type
            httpContext.Response.StatusCode = (int)problemDetails.Status;

            // 6: Create a ProblemDetails response using the IProblemDetailsService
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = domainException,
                ProblemDetails = problemDetails
            });
        }
    }
}