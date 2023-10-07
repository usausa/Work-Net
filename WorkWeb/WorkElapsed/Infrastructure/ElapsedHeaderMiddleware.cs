namespace WorkTiming.Infrastructure;

using System.Diagnostics;

public sealed class ElapsedHeaderMiddleware
{
    private readonly RequestDelegate next;

    public ElapsedHeaderMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Elapsed"] = $"{stopwatch.ElapsedMilliseconds}";
            return Task.CompletedTask;
        });

        await next(context);
    }
}