using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PayFlow.API.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
            => _healthCheckService = healthCheckService;

        [HttpGet]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

            var response = new HealthResponse(
                Status: report.Status.ToString(),
                TotalDuration: report.TotalDuration.TotalMilliseconds,
                Dependencies: report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new DependencyHealth(
                        Status: entry.Value.Status.ToString(),
                        Duration: entry.Value.Duration.TotalMilliseconds,
                        Description: entry.Value.Description
                    )));

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        public sealed record HealthResponse(
            string Status,
            double TotalDuration,
            Dictionary<string, DependencyHealth> Dependencies);

        public sealed record DependencyHealth(
            string Status,
            double Duration,
            string? Description);
    }
}