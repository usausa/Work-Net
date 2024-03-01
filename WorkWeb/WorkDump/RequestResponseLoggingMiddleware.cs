namespace WorkDump;

using System.Text;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        return builder;
    }
}


public class RequestResponseLoggingMiddleware
{
    private static readonly string[] TargetTypes =
    [
        "application/json",
        "text/json",
        "application/xml",
        "text/xml"
    ];

    private static readonly Encoding TextEncoding = Encoding.UTF8;

    private readonly RequestDelegate next;

    private readonly ILogger<RequestResponseLoggingMiddleware> logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (logger.IsEnabled(LogLevel.Debug) &&
            !String.IsNullOrEmpty(context.Request.ContentType) &&
            TargetTypes.Contains(context.Request.ContentType))
        {
            var requestBody = await ReadRequestBody(context.Request).ConfigureAwait(false);
            if (requestBody.Length > 0)
            {
                logger.LogDebug("Request dump. dump=[{Dump}]", TextEncoding.GetString(requestBody));
            }

            var originalBodyStream = context.Response.Body;
#pragma warning disable CA2007
            await using var responseBodyStream = new MemoryStream();
#pragma warning restore CA2007
            context.Response.Body = responseBodyStream;

            await next(context).ConfigureAwait(false);

            var responseBody = await ReadResponseBody(context.Response).ConfigureAwait(false);
            if (responseBody.Length > 0)
            {
                logger.LogDebug("Response dump. dump=[{Dump}]", TextEncoding.GetString(responseBody));
                await responseBodyStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            }
        }
        else
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static async ValueTask<byte[]> ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering();

#pragma warning disable CA2007
        await using var memoryStream = new MemoryStream((int)(request.ContentLength ?? 0));
#pragma warning restore CA2007
        await request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);

        request.Body.Seek(0, SeekOrigin.Begin);

        return memoryStream.ToArray();
    }

    private static async ValueTask<byte[]> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);

#pragma warning disable CA2007
        await using var memoryStream = new MemoryStream();
#pragma warning restore CA2007
        await response.Body.CopyToAsync(memoryStream).ConfigureAwait(false);

        response.Body.Seek(0, SeekOrigin.Begin);

        return memoryStream.ToArray();
    }
}
