namespace WorkPipeline.Infrastructure;

using Microsoft.Extensions.DiagnosticAdapter;

#pragma warning disable IDE0060
public class AnalysisDiagnosticAdapter
{
    private readonly ILogger<AnalysisDiagnosticAdapter> logger;

    public AnalysisDiagnosticAdapter(ILogger<AnalysisDiagnosticAdapter> logger)
    {
        this.logger = logger;
    }

    [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
    public void OnMiddlewareStarting(HttpContext httpContext, string name, Guid instance, long timestamp)
    {
        if (name == "Microsoft.AspNetCore.MiddlewareAnalysis.AnalysisMiddleware")
        {
            return;
        }

        logger.LogInformation($"Starting: {name} : Path=[{httpContext.Request.Path}]");
    }

    [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
    public void OnMiddlewareFinished(HttpContext httpContext, string name, Guid instance, long timestamp, long duration)
    {
        if (name == "Microsoft.AspNetCore.MiddlewareAnalysis.AnalysisMiddleware")
        {
            return;
        }

        logger.LogInformation($"MiddlewareFinished: {name} : Status=[{httpContext.Response.StatusCode}]");
    }

    [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
    public void OnMiddlewareException(Exception exception, HttpContext httpContext, string name, Guid instance, long timestamp, long duration)
    {
        if (name == "Microsoft.AspNetCore.MiddlewareAnalysis.AnalysisMiddleware")
        {
            return;
        }

        logger.LogInformation($"MiddlewareException: {name} : {exception.Message}");
    }
}
#pragma warning restore IDE0060
