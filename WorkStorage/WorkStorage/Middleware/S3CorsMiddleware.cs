using System.Text.Json;

using WorkStorage.Models;

namespace WorkStorage.Middleware;

/// <summary>
/// Applies per-bucket CORS headers based on configurations stored by the S3 controller.
/// Handles OPTIONS preflight requests and adds CORS headers to matching responses.
/// </summary>
public sealed class S3CorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _metaBasePath;

    public S3CorsMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var basePath = configuration["Storage:BasePath"]
            ?? throw new InvalidOperationException("Storage:BasePath is not configured.");
        _metaBasePath = Path.Combine(Path.GetFullPath(basePath), ".meta");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            await _next(context);
            return;
        }

        var bucket = ExtractBucket(context.Request.Path);
        if (string.IsNullOrEmpty(bucket))
        {
            await _next(context);
            return;
        }

        var rules = LoadCorsRules(bucket);
        if (rules is null || rules.Count == 0)
        {
            await _next(context);
            return;
        }

        // Determine the effective method (preflight uses Access-Control-Request-Method)
        var effectiveMethod = context.Request.Method;
        if (effectiveMethod == "OPTIONS")
        {
            var requested = context.Request.Headers["Access-Control-Request-Method"].ToString();
            if (!string.IsNullOrEmpty(requested))
                effectiveMethod = requested;
        }

        var rule = FindMatchingRule(rules, origin, effectiveMethod);
        if (rule is null)
        {
            await _next(context);
            return;
        }

        // Common CORS headers
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers.Vary = "Origin";

        if (rule.ExposeHeaders.Count > 0)
            context.Response.Headers["Access-Control-Expose-Headers"] =
                string.Join(", ", rule.ExposeHeaders);

        // Preflight response
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers["Access-Control-Allow-Methods"] =
                string.Join(", ", rule.AllowedMethods);

            if (rule.AllowedHeaders.Count > 0)
                context.Response.Headers["Access-Control-Allow-Headers"] =
                    string.Join(", ", rule.AllowedHeaders);

            if (rule.MaxAgeSeconds > 0)
                context.Response.Headers["Access-Control-Max-Age"] =
                    rule.MaxAgeSeconds.ToString();

            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        await _next(context);
    }

    private static string? ExtractBucket(PathString path)
    {
        var value = path.Value?.TrimStart('/') ?? "";
        if (string.IsNullOrEmpty(value))
            return null;

        var slashIndex = value.IndexOf('/');
        return slashIndex > 0 ? value[..slashIndex] : value;
    }

    private List<CorsRule>? LoadCorsRules(string bucket)
    {
        var corsPath = Path.Combine(_metaBasePath, ".buckets", bucket + "-cors.json");
        if (!File.Exists(corsPath))
            return null;

        var json = File.ReadAllText(corsPath);
        return JsonSerializer.Deserialize<List<CorsRule>>(json);
    }

    private static CorsRule? FindMatchingRule(
        List<CorsRule> rules, string origin, string method)
    {
        return rules.FirstOrDefault(r =>
            r.AllowedOrigins.Any(o =>
                o == "*" || string.Equals(o, origin, StringComparison.OrdinalIgnoreCase))
            && r.AllowedMethods.Any(m =>
                string.Equals(m, method, StringComparison.OrdinalIgnoreCase)));
    }
}
