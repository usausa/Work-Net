namespace WorkHealthy.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class DiskHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        var drive = new DriveInfo("C:\\");
        var free = (double)drive.TotalFreeSpace / drive.TotalSize;
        return free < 0.1
            ? Task.FromResult(HealthCheckResult.Unhealthy())
            : Task.FromResult(HealthCheckResult.Healthy());
    }
}
