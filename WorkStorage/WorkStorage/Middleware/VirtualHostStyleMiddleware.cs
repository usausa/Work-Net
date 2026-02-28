namespace WorkStorage.Middleware;

/// <summary>
/// Rewrites virtual-hosted style S3 requests into path-style so that the
/// controller routes work unchanged.
/// <para>
/// Virtual-hosted: <c>http://{bucket}.s3.localhost:5128/{key}</c><br/>
/// Path-style:     <c>http://localhost:5128/{bucket}/{key}</c>
/// </para>
/// If the <c>Host</c> header has a subdomain relative to the configured
/// base hostname (<c>Storage:BaseHostname</c>, default <c>s3.localhost</c>),
/// the subdomain is extracted as the bucket name and prepended to the path.
/// Requests that already use path-style pass through untouched.
/// </summary>
public sealed class VirtualHostStyleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _baseHostname;

    public VirtualHostStyleMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _baseHostname = configuration["Storage:BaseHostname"] ?? "s3.localhost";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host; // excludes port

        // Check if the host ends with ".{baseHostname}" (e.g. "my-bucket.s3.localhost")
        var suffix = "." + _baseHostname;
        if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && host.Length > suffix.Length)
        {
            var bucket = host[..^suffix.Length];
            context.Request.Path = "/" + bucket + context.Request.Path;
        }

        await _next(context);
    }
}
