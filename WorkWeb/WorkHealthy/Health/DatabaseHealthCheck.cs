namespace WorkHealthy.Health;

using Smart.Data;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbProvider dbProvider;

    public DatabaseHealthCheck(IDbProvider dbProvider)
    {
        this.dbProvider = dbProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await using var con = dbProvider.CreateConnection();
            await con.OpenAsync(cancellationToken);

            await using var command = con.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
