namespace WorkCron;

public sealed class WorkerService : SchedulerService
{
    private readonly ILogger<WorkerService> log;

    public WorkerService(
        ILogger<WorkerService> log,
        SchedulerConfig<WorkerService> config)
        : base(config.Expression, config.TimeZoneInfo)
    {
        this.log = log;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("WorkerService start.");
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        log.LogInformation("WorkerService stop.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
    protected override ValueTask DoWorkAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("WorkerService work.");
        return ValueTask.CompletedTask;
    }
}
