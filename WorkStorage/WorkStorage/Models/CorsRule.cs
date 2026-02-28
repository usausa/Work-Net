namespace WorkStorage.Models;

/// <summary>
/// Represents a single CORS rule for a bucket, matching the S3 CORSRule schema.
/// Serialized as JSON for persistence and used by <see cref="Middleware.S3CorsMiddleware"/>
/// at runtime.
/// </summary>
public sealed class CorsRule
{
    public List<string> AllowedOrigins { get; init; } = [];
    public List<string> AllowedMethods { get; init; } = [];
    public List<string> AllowedHeaders { get; init; } = [];
    public List<string> ExposeHeaders { get; init; } = [];
    public int MaxAgeSeconds { get; init; }
}
