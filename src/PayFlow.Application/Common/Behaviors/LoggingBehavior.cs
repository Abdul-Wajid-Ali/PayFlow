using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PayFlow.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // 1: Get request name for logging context
            var requestName = typeof(TRequest).Name;

            // 2: Log incoming request with payload
            _logger.LogInformation("Handling {RequestName} with content: {@Request}", requestName, request);

            // 3: Start performance timer
            var stopWatch = Stopwatch.StartNew();

            try
            {
                // 4: Execute next pipeline behavior or handler
                var response = await next(cancellationToken);

                // 5: Stop timer after successful execution
                stopWatch.Stop();

                // 6: Log successful execution with duration and response
                _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms with response: {@Response}",
                    requestName, stopWatch.ElapsedMilliseconds, response);

                // 7: Return handler response
                return response;
            }
            catch (Exception ex)
            {
                // 8: Stop timer on exception
                stopWatch.Stop();

                // 9: Log error with execution time and exception details
                _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMilliseconds}ms",
                    requestName, stopWatch.ElapsedMilliseconds);

                // 10: Rethrow exception to preserve pipeline behavior
                throw;
            }
        }
    }
}