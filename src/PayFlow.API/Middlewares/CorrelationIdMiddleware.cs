using Serilog.Context;

namespace PayFlow.API.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
            => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            // 1: Try to read correlation ID from incoming request headers
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
            {
                // 2: Generate a new correlation ID if the request doesn't contain one
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[CorrelationIdHeader] = correlationId;
            }

            // 3: Attach correlation ID to response headers before response is sent
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // 4: Push correlation ID into logging scope for traceable logs
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                // 5: Continue request pipeline to next middleware
                await _next(context);
            }
        }
    }
}