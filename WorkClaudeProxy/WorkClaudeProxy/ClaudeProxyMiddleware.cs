using System.Text;
using System.Text.Json;

namespace WorkClaudeProxy;

internal sealed class ClaudeProxyMiddleware
{
    public const string UpstreamHeadersKey = "ClaudeProxy_UpstreamHeaders";

    private static readonly Dictionary<string, int> ContextWindowSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "claude-opus-4",     200_000 },
        { "claude-sonnet-4",   200_000 },
        { "claude-haiku-4",    200_000 },
        { "claude-3-7-sonnet", 200_000 },
        { "claude-3-5-sonnet", 200_000 },
        { "claude-3-5-haiku",  200_000 },
        { "claude-3-opus",     200_000 },
        { "claude-3-sonnet",   200_000 },
        { "claude-3-haiku",    200_000 },
    };

    private readonly RequestDelegate next;
    private readonly ILogger<ClaudeProxyMiddleware> logger;
    private readonly DashboardImageStore imageStore;
    private readonly Lock stateLock = new();
    private DisplayState? lastState;

    public ClaudeProxyMiddleware(RequestDelegate next, ILogger<ClaudeProxyMiddleware> logger, DashboardImageStore imageStore)
    {
        this.next = next;
        this.logger = logger;
        this.imageStore = imageStore;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        var buffer = new MemoryStream();
        var teeStream = new TeeStream(originalBody, buffer);
        context.Response.Body = teeStream;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        await LogResponseAsync(context, buffer);
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream buffer)
    {
        var statusCode = context.Response.StatusCode;
        var contentType = context.Response.ContentType ?? string.Empty;

        buffer.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(buffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();

        var upstreamHeaders = context.Items[UpstreamHeadersKey] as Dictionary<string, string>;
        var rateLimitInfo = ParseRateLimitHeaders(upstreamHeaders);
        var (usageInfo, model) = ParseResponseBody(bodyText, contentType);

        DisplayState stateToLog;
        lock (stateLock)
        {
            // 今回取得できなかった項目は最後に保持している値で補完してマージ
            var merged = new DisplayState(
                model ?? lastState?.Model,
                new UsageInfo(
                    usageInfo.InputTokens ?? lastState?.Usage.InputTokens,
                    usageInfo.OutputTokens ?? lastState?.Usage.OutputTokens,
                    usageInfo.CacheCreationInputTokens ?? lastState?.Usage.CacheCreationInputTokens,
                    usageInfo.CacheReadInputTokens ?? lastState?.Usage.CacheReadInputTokens
                ),
                new RateLimitInfo(
                    rateLimitInfo.FiveHourStatus ?? lastState?.RateLimit.FiveHourStatus,
                    rateLimitInfo.FiveHourUtilization ?? lastState?.RateLimit.FiveHourUtilization,
                    rateLimitInfo.FiveHourReset ?? lastState?.RateLimit.FiveHourReset,
                    rateLimitInfo.SevenDayStatus ?? lastState?.RateLimit.SevenDayStatus,
                    rateLimitInfo.SevenDayUtilization ?? lastState?.RateLimit.SevenDayUtilization,
                    rateLimitInfo.SevenDayReset ?? lastState?.RateLimit.SevenDayReset,
                    rateLimitInfo.OverageStatus ?? lastState?.RateLimit.OverageStatus,
                    rateLimitInfo.OverageDisabledReason ?? lastState?.RateLimit.OverageDisabledReason
                )
            );
            if (merged == lastState)
                return;
            lastState = merged;
            stateToLog = merged;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"  ┌─── {context.Request.Method} {context.Request.Path} [{statusCode}]");

        if (stateToLog.Model is not null)
            sb.AppendLine($"  │  Model: {stateToLog.Model}");

        var u = stateToLog.Usage;
        if (u.InputTokens is not null)
        {
            var contextWindowSize = GetContextWindowSize(stateToLog.Model);
            sb.AppendLine("  │  Token Usage:");

            var cacheRead    = u.CacheReadInputTokens ?? 0;
            var cacheCreated = u.CacheCreationInputTokens ?? 0;
            if (cacheRead > 0 || cacheCreated > 0)
            {
                sb.AppendLine($"  │    Input:   {u.InputTokens.Value,8:N0}  (cache read: {cacheRead:N0} / created: {cacheCreated:N0})");
            }
            else
            {
                sb.AppendLine($"  │    Input:   {u.InputTokens.Value,8:N0}");
            }

            if (u.OutputTokens > 0)
                sb.AppendLine($"  │    Output:  {u.OutputTokens.GetValueOrDefault(),8:N0}");

            if (contextWindowSize > 0)
            {
                // コンテキストウィンドウ使用量はキャッシュ読み込み分も含む入力トークン全体で算出
                var totalInputTokens = u.InputTokens.Value + cacheRead + cacheCreated;
                var percentage = (double)totalInputTokens / contextWindowSize * 100.0;
                sb.AppendLine($"  │    Context: {totalInputTokens:N0} / {contextWindowSize:N0} ({percentage:F1}% of context window used)");
            }
        }

        var rl = stateToLog.RateLimit;
        if (rl.FiveHourStatus is not null || rl.SevenDayStatus is not null)
        {
            sb.AppendLine("  │  Rate Limits:");

            if (rl.FiveHourStatus is not null)
            {
                var resetStr = rl.FiveHourReset?.ToLocalTime().ToString("HH:mm:ss") ?? "—";
                sb.AppendLine($"  │    5h:  {(rl.FiveHourUtilization ?? 0) * 100,5:F1}%  [{rl.FiveHourStatus}]  (resets {resetStr})");
            }

            if (rl.SevenDayStatus is not null)
            {
                var resetStr = rl.SevenDayReset?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
                sb.AppendLine($"  │    7d:  {(rl.SevenDayUtilization ?? 0) * 100,5:F1}%  [{rl.SevenDayStatus}]  (resets {resetStr})");
            }
        }

        sb.Append("  └────────────────────────────────────────────────");

        logger.LogInformation("{Info}", sb.ToString());

        imageStore.UpdateState(stateToLog);
    }

    internal static int GetContextWindowSize(string? model)
    {
        if (model is null)
            return 0;

        foreach (var (prefix, size) in ContextWindowSizes)
        {
            if (model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return size;
        }

        return 0;
    }

    private static RateLimitInfo ParseRateLimitHeaders(Dictionary<string, string>? headers)
    {
        if (headers is null)
            return RateLimitInfo.Empty;

        var fiveHourStatus = ParseStringHeader(headers, "anthropic-ratelimit-unified-5h-status");
        var fiveHourReset = ParseUnixTimestampHeader(headers, "anthropic-ratelimit-unified-5h-reset");
        var fiveHourUtilization = ParseDoubleHeader(headers, "anthropic-ratelimit-unified-5h-utilization");
        var sevenDayStatus = ParseStringHeader(headers, "anthropic-ratelimit-unified-7d-status");
        var sevenDayReset = ParseUnixTimestampHeader(headers, "anthropic-ratelimit-unified-7d-reset");
        var sevenDayUtilization = ParseDoubleHeader(headers, "anthropic-ratelimit-unified-7d-utilization");
        var overageStatus = ParseStringHeader(headers, "anthropic-ratelimit-unified-overage-status");
        var overageDisabledReason = ParseStringHeader(headers, "anthropic-ratelimit-unified-overage-disabled-reason");

        return new RateLimitInfo(
            fiveHourStatus, fiveHourUtilization, fiveHourReset,
            sevenDayStatus, sevenDayUtilization, sevenDayReset,
            overageStatus, overageDisabledReason
        );
    }

    private static string? ParseStringHeader(Dictionary<string, string> headers, string name)
        => headers.TryGetValue(name, out var value) ? value : null;

    private static double? ParseDoubleHeader(Dictionary<string, string> headers, string name)
        => headers.TryGetValue(name, out var value) && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;

    private static DateTimeOffset? ParseUnixTimestampHeader(Dictionary<string, string> headers, string name)
        => headers.TryGetValue(name, out var value) && long.TryParse(value, out var result) ? DateTimeOffset.FromUnixTimeSeconds(result) : null;

    private static (UsageInfo usage, string? model) ParseResponseBody(string body, string contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (UsageInfo.Empty, null);

        try
        {
            if (contentType.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
                return ParseSseBody(body);

            if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return ParseJsonBody(body);
        }
        catch
        {
            // ボディ解析はベストエフォートのため、エラーは無視する
        }

        return (UsageInfo.Empty, null);
    }

    private static (UsageInfo usage, string? model) ParseSseBody(string body)
    {
        int? inputTokens = null;
        int? outputTokens = null;
        int? cacheCreationInputTokens = null;
        int? cacheReadInputTokens = null;
        string? model = null;

        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (!trimmed.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var json = trimmed[6..];
            if (json == "[DONE]")
                continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp))
                    continue;

                var eventType = typeProp.GetString();
                if (eventType == "message_start")
                {
                    if (root.TryGetProperty("message", out var message))
                    {
                        if (message.TryGetProperty("model", out var modelProp))
                            model = modelProp.GetString();

                        if (message.TryGetProperty("usage", out var usage))
                        {
                            inputTokens = GetInt(usage, "input_tokens");
                            cacheCreationInputTokens = GetInt(usage, "cache_creation_input_tokens");
                            cacheReadInputTokens = GetInt(usage, "cache_read_input_tokens");
                        }
                    }
                }
                else if (eventType == "message_delta")
                {
                    if (root.TryGetProperty("usage", out var usage))
                        outputTokens = GetInt(usage, "output_tokens");
                }
            }
            catch (JsonException)
            {
                // 不正なSSEイベントはスキップ
            }
        }

        return (new UsageInfo(inputTokens, outputTokens, cacheCreationInputTokens, cacheReadInputTokens), model);
    }

    private static (UsageInfo usage, string? model) ParseJsonBody(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var model = root.TryGetProperty("model", out var modelProp) ? modelProp.GetString() : null;

        // /v1/messages レスポンス
        if (root.TryGetProperty("usage", out var usage))
        {
            return (new UsageInfo(
                GetInt(usage, "input_tokens"),
                GetInt(usage, "output_tokens"),
                GetInt(usage, "cache_creation_input_tokens"),
                GetInt(usage, "cache_read_input_tokens")
            ), model);
        }

        // /v1/messages/count_tokens レスポンス
        if (root.TryGetProperty("input_tokens", out var inputTokensProp) && inputTokensProp.TryGetInt32(out var countTokens))
            return (new UsageInfo(countTokens, null, null, null), model);

        return (UsageInfo.Empty, model);
    }

    private static int GetInt(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value) ? value : 0;
}

internal sealed record DisplayState(
    string? Model,
    UsageInfo Usage,
    RateLimitInfo RateLimit
)
{
    public static readonly DisplayState Empty = new(null, UsageInfo.Empty, RateLimitInfo.Empty);
}

internal sealed record UsageInfo(
    int? InputTokens,
    int? OutputTokens,
    int? CacheCreationInputTokens,
    int? CacheReadInputTokens
)
{
    public static readonly UsageInfo Empty = new(null, null, null, null);
}

internal sealed record RateLimitInfo(
    string? FiveHourStatus,
    double? FiveHourUtilization,
    DateTimeOffset? FiveHourReset,
    string? SevenDayStatus,
    double? SevenDayUtilization,
    DateTimeOffset? SevenDayReset,
    string? OverageStatus,
    string? OverageDisabledReason
)
{
    public static readonly RateLimitInfo Empty = new(null, null, null, null, null, null, null, null);
}
