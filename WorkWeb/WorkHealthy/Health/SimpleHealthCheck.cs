namespace WorkHealthy.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class SimpleHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return DateTime.Now.Second % 2 == 0
            ? Task.FromResult(HealthCheckResult.Healthy("Simple check success."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Simple check failed."));
    }
}
