using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PayFlow.API.RateLimiting
{
    public static class TransferRateLimitRejectionHandler
    {
        public static async ValueTask HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            _ = cancellationToken;

            // 1: Set the response status code and content type
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";

            // 2: Attach Retry-After header if the rate limiter lease provides that metadata
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();

            // 3: Resolve IProblemDetailsService from the request DI container
            var problemDetailsService = context.HttpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

            // 4: Create a ProblemDetails object describing the rate limit violation
            var problemDetails = new ProblemDetails
            {
                Type = "TooManyRequests",
                Title = "Too many requests.",
                Detail = "You have exceeded the transfer limit of 5 requests per minute.",
                Status = StatusCodes.Status429TooManyRequests,
                Instance = context.HttpContext.Request.Path
            };

            // 5: Write the ProblemDetails response using the IProblemDetailsService
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetails
            });
        }
    }
}