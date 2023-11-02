namespace WorkCron;

// ReSharper disable once UnusedTypeParameter
public class SchedulerConfig<T>
{
    public string Expression { get; set; } = string.Empty;

    public TimeZoneInfo TimeZoneInfo { get; set; } = TimeZoneInfo.Local;
}
