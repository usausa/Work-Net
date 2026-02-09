namespace WorkLinuxBleScan2;

using System.Diagnostics.Tracing;
using System.Text;

[EventSource(Name = "WorkLinuxBleScan2")]
internal sealed class BleEventSource : EventSource
{
    public static readonly BleEventSource Log = new();

    [Event(1, Level = EventLevel.Informational, Message = "{0}")]
    public void Info(string message) => WriteEvent(1, message);

    [Event(2, Level = EventLevel.Warning, Message = "{0}")]
    public void Warn(string message) => WriteEvent(2, message);

    [Event(3, Level = EventLevel.Error, Message = "{0}")]
    public void Error(string message) => WriteEvent(3, message);
}

public sealed class BleDiagnostics : IAsyncDisposable
{
    private readonly string logFilePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private const long MaxBytes = 2 * 1024 * 1024;

    public BleDiagnostics(string? logDirectory = null)
    {
        logDirectory ??= Environment.CurrentDirectory;
        Directory.CreateDirectory(logDirectory);
        logFilePath = Path.Combine(logDirectory, "blescan.log");
    }

    public string LogFilePath => logFilePath;

    public void Info(string message) => Write("INF", message);
    public void Warn(string message) => Write("WRN", message);
    public void Error(string message) => Write("ERR", message);

    public void Exception(Exception ex, string context) => Write("EX", $"{context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O}\t{level}\t{message}";

        try
        {
            switch (level)
            {
                case "ERR":
                case "EX":
                    BleEventSource.Log.Error(line);
                    break;
                case "WRN":
                    BleEventSource.Log.Warn(line);
                    break;
                default:
                    BleEventSource.Log.Info(line);
                    break;
            }
        }
        catch
        {
            // ignore EventSource failures
        }

        _ = Task.Run(async () =>
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                RotateIfNeededNoThrow();
                await File.AppendAllTextAsync(logFilePath, line + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
            }
            catch
            {
                // ignore logging failures
            }
            finally
            {
                gate.Release();
            }
        });
    }

    private void RotateIfNeededNoThrow()
    {
        try
        {
            var fi = new FileInfo(logFilePath);
            if (!fi.Exists || fi.Length <= MaxBytes)
            {
                return;
            }

            var backup = logFilePath + ".1";
            if (File.Exists(backup))
            {
                File.Delete(backup);
            }
            File.Move(logFilePath, backup);
        }
        catch
        {
            // ignore
        }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        gate.Release();
        gate.Dispose();
    }
}
