namespace WorkCron;

using Cronos;

public abstract class SchedulerService : BackgroundService
{
    private readonly CronExpression expression;

    private readonly TimeZoneInfo timeZoneInfo;

    protected SchedulerService(string cronExpression, TimeZoneInfo timeZoneInfo)
    {
        expression = CronExpression.Parse(cronExpression);
        this.timeZoneInfo = timeZoneInfo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var next = expression.GetNextOccurrence(DateTimeOffset.Now, timeZoneInfo);
        while (!stoppingToken.IsCancellationRequested && next.HasValue)
        {
            try
            {
                var delay = next.Value - DateTimeOffset.Now;
                await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await DoWorkAsync(stoppingToken);
                }
            }
            catch
            {
                // Ignore
            }

            next = expression.GetNextOccurrence(DateTimeOffset.Now, timeZoneInfo);
        }
    }

    protected abstract ValueTask DoWorkAsync(CancellationToken cancellationToken);
}
