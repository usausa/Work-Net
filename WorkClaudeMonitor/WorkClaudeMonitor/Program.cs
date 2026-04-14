using System.Text.Json;

// =================== 初期化 ===================
var store = new UsageStore();

var pricing = new ModelPricingTable();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Services.AddSingleton(store);
builder.Services.AddSingleton(pricing);
builder.Services.AddHostedService<DisplayWorker>();
var app = builder.Build();

// =================== OTLP エンドポイント ===================
app.MapPost("/v1/traces", async (HttpContext ctx) =>
{
    var json = await ReadBodyAsync(ctx);
    if (store.ApplyTraces(json)) store.PrintUsage(pricing);
    return Results.Ok();
});

app.MapPost("/v1/metrics", async (HttpContext ctx) =>
{
    var json = await ReadBodyAsync(ctx);
    if (store.ApplyMetrics(json)) store.PrintUsage(pricing);
    return Results.Ok();
});

app.MapPost("/v1/logs", async (HttpContext ctx) => { _ = await ReadBodyAsync(ctx); return Results.Ok(); });

Console.WriteLine("Claude Code Usage Monitor  (OTLP: http://localhost:4318)");
Console.WriteLine(new string('─', 52));

app.Run("http://localhost:4318");

// =================== ユーティリティ ===================
static async Task<string> ReadBodyAsync(HttpContext ctx)
{
    using var reader = new StreamReader(ctx.Request.Body);
    return await reader.ReadToEndAsync();
}

// =================== Worker (定期表示) ===================
class DisplayWorker(UsageStore store, ModelPricingTable pricing) : BackgroundService
{
    // テレメトリ受信間隔(デフォルト60秒)に合わせて定期表示
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            if (store.HasData)
                store.PrintUsage(pricing, periodic: true);
        }
    }
}

// =================== 使用量ストア ===================
class UsageStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ModelMetrics> _metricsByModel
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TraceTokens> _traceByModel
        = new(StringComparer.OrdinalIgnoreCase);
    private double _activeTimeSec;

    public DateTime SessionStart { get; } = DateTime.Now;
    public bool HasMetrics => _metricsByModel.Count > 0;
    public bool HasData    => _metricsByModel.Count > 0 || _traceByModel.Count > 0;

    // ---- メトリクス適用 ----
    public bool ApplyMetrics(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("resourceMetrics", out var resourceMetrics))
                return false;

            bool updated = false;
            foreach (var rm in resourceMetrics.EnumerateArray())
            {
                if (!rm.TryGetProperty("scopeMetrics", out var scopeMetrics)) continue;
                foreach (var sm in scopeMetrics.EnumerateArray())
                {
                    if (!sm.TryGetProperty("metrics", out var metrics)) continue;
                    foreach (var metric in metrics.EnumerateArray())
                    {
                        if (!metric.TryGetProperty("name", out var nameEl)) continue;
                        var name = nameEl.GetString() ?? "";

                        JsonElement dataPoints = default;
                        if      (metric.TryGetProperty("sum",   out var s) && s.TryGetProperty("dataPoints", out dataPoints)) { }
                        else if (metric.TryGetProperty("gauge", out var g) && g.TryGetProperty("dataPoints", out dataPoints)) { }
                        else continue;

                        foreach (var dp in dataPoints.EnumerateArray())
                        {
                            double value;
                            if (dp.TryGetProperty("asDouble", out var dv))
                            {
                                value = dv.GetDouble();
                            }
                            else if (dp.TryGetProperty("asInt", out var iv))
                            {
                                try
                                {
                                    value = iv.ValueKind == JsonValueKind.String
                                        ? double.Parse(iv.GetString()!) : iv.GetDouble();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[WARN][Metrics] {name}: asInt の変換に失敗 (値={iv}) — {ex.Message}");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[WARN][Metrics] {name}: asDouble/asInt が見つかりません");
                                continue;
                            }

                            var attrs = dp.TryGetProperty("attributes", out var a) ? a : default;

                            switch (name)
                            {
                                case "claude_code.token.usage":
                                {
                                    var model = OtlpAttr.GetString(attrs, "model");
                                    var type  = OtlpAttr.GetString(attrs, "type");
                                    if (model is null) { Console.WriteLine($"[WARN][Metrics] {name}: 属性 'model' がありません"); break; }
                                    if (type  is null) { Console.WriteLine($"[WARN][Metrics] {name}: 属性 'type' がありません (model={model})"); break; }
                                    lock (_lock)
                                    {
                                        if (!_metricsByModel.TryGetValue(model, out var m))
                                            _metricsByModel[model] = m = new ModelMetrics();
                                        switch (type)
                                        {
                                            case "input":         m.Input      = (long)value; break;
                                            case "output":        m.Output     = (long)value; break;
                                            case "cacheRead":     m.CacheRead  = (long)value; break;
                                            case "cacheCreation": m.CacheWrite = (long)value; break;
                                            default: Console.WriteLine($"[WARN][Metrics] {name}: 未知の type='{type}'"); break;
                                        }
                                    }
                                    updated = true;
                                    break;
                                }
                                case "claude_code.cost.usage":
                                {
                                    var model = OtlpAttr.GetString(attrs, "model");
                                    if (model is null) { Console.WriteLine($"[WARN][Metrics] {name}: 属性 'model' がありません"); break; }
                                    lock (_lock)
                                    {
                                        if (!_metricsByModel.TryGetValue(model, out var m))
                                            _metricsByModel[model] = m = new ModelMetrics();
                                        m.Cost = (decimal)value;
                                    }
                                    updated = true;
                                    break;
                                }
                                case "claude_code.active_time.total":
                                    lock (_lock) { _activeTimeSec = value; }
                                    updated = true;
                                    break;
                            }
                        }
                    }
                }
            }
            return updated;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR][Metrics] JSON パース失敗 — {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR][Metrics] 予期しないエラー — {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    // ---- トレース適用 (フォールバック) ----
    public bool ApplyTraces(string json)
    {
        lock (_lock) { if (_metricsByModel.Count > 0) return false; }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("resourceSpans", out var resourceSpans)) return false;

            bool updated = false;
            foreach (var rs in resourceSpans.EnumerateArray())
            {
                if (!rs.TryGetProperty("scopeSpans", out var scopeSpans)) continue;
                foreach (var ss in scopeSpans.EnumerateArray())
                {
                    if (!ss.TryGetProperty("spans", out var spans)) continue;
                    foreach (var span in spans.EnumerateArray())
                    {
                        if (!span.TryGetProperty("attributes", out var attrs)) continue;

                        var model = OtlpAttr.GetString(attrs, "gen_ai.response.model")
                                 ?? OtlpAttr.GetString(attrs, "gen_ai.request.model");
                        if (model is null)
                        {
                            Console.WriteLine("[WARN][Traces] gen_ai.response.model / gen_ai.request.model が見つかりません。スキップします。");
                            continue;
                        }

                        long input      = OtlpAttr.GetLong(attrs, "gen_ai.usage.input_tokens");
                        long output     = OtlpAttr.GetLong(attrs, "gen_ai.usage.output_tokens");
                        long cacheRead  = OtlpAttr.GetLong(attrs, "gen_ai.usage.cache_read_input_tokens");
                        long cacheWrite = OtlpAttr.GetLong(attrs, "gen_ai.usage.cache_creation_input_tokens");
                        if (input == 0 && output == 0)
                        {
                            Console.WriteLine($"[WARN][Traces] model={model}: input/output が両方 0 のためスキップします。");
                            continue;
                        }

                        lock (_lock)
                        {
                            if (!_traceByModel.TryGetValue(model, out var t))
                                _traceByModel[model] = t = new TraceTokens();
                            t.Input      += input;
                            t.Output     += output;
                            t.CacheRead  += cacheRead;
                            t.CacheWrite += cacheWrite;
                        }
                        updated = true;
                    }
                }
            }
            return updated;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR][Traces] JSON パース失敗 — {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR][Traces] 予期しないエラー — {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    // ---- コンソール表示 ----
    public void PrintUsage(ModelPricingTable pricing, bool periodic = false)
    {
        lock (_lock)
        {
            Console.WriteLine(new string('─', 52));
            if (periodic) Console.WriteLine($"[定期表示 {DateTime.Now:HH:mm:ss}]");
            Console.WriteLine(BuildSummary(pricing));
        }
    }

    // ---- サマリ文字列生成 ----
    public string BuildSummary(ModelPricingTable pricing)
    {
        bool hasMetrics = _metricsByModel.Count > 0;
        bool hasTraces  = _traceByModel.Count  > 0;

        if (!hasMetrics && !hasTraces)
            return $"(waiting for telemetry...)  since {SessionStart:HH:mm:ss}";

        long totalIn = 0, totalOut = 0, totalCR = 0, totalCW = 0;
        decimal totalCost = 0;

        if (hasMetrics)
        {
            foreach (var (_, m) in _metricsByModel)
            {
                totalIn   += m.Input;
                totalOut  += m.Output;
                totalCR   += m.CacheRead;
                totalCW   += m.CacheWrite;
                totalCost += m.Cost;
            }
        }
        else
        {
            foreach (var (model, t) in _traceByModel)
            {
                totalIn   += t.Input;
                totalOut  += t.Output;
                totalCR   += t.CacheRead;
                totalCW   += t.CacheWrite;
                totalCost += pricing.CalcCost(model, t.Input, t.Output, t.CacheRead, t.CacheWrite);
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Token Usage (since {SessionStart:yyyy-MM-dd HH:mm:ss})");
        sb.AppendLine(new string('─', 48));
        sb.AppendLine($"  {"Input tokens:",-22} {totalIn,12:N0}");
        sb.AppendLine($"  {"Output tokens:",-22} {totalOut,12:N0}");
        sb.AppendLine($"  {"Cache read tokens:",-22} {totalCR,12:N0}");
        sb.AppendLine($"  {"Cache write tokens:",-22} {totalCW,12:N0}");
        sb.AppendLine(new string('─', 48));
        sb.AppendLine($"  {"Cost estimate:",-22} ${totalCost,11:F4}");
        if (_activeTimeSec > 0)
            sb.AppendLine($"  {"Active time:",-22} {FormatTime(_activeTimeSec),12}");
        sb.AppendLine();

        sb.AppendLine("  Breakdown by model:");
        if (hasMetrics)
        {
            foreach (var (model, m) in _metricsByModel.OrderBy(x => x.Key))
                sb.AppendLine($"    {ShortenModel(model),-24} ${m.Cost:F4}  ({FormatN(m.Input)} in, {FormatN(m.Output)} out)");
        }
        else
        {
            foreach (var (model, t) in _traceByModel.OrderBy(x => x.Key))
            {
                var cost = pricing.CalcCost(model, t.Input, t.Output, t.CacheRead, t.CacheWrite);
                sb.AppendLine($"    {ShortenModel(model),-24} ${cost:F4}  ({FormatN(t.Input)} in, {FormatN(t.Output)} out)");
            }
        }

        sb.AppendLine();
        sb.Append($"  Last updated: {DateTime.Now:HH:mm:ss}");
        if (!hasMetrics) sb.Append("  (trace fallback)");
        return sb.ToString();
    }

    private static string FormatN(long n) =>
        n >= 1_000_000 ? $"{n / 1_000_000.0:F2}M" :
        n >= 1_000     ? $"{n / 1_000.0:F1}k"     : $"{n}";

    private static string FormatTime(double sec) =>
        sec >= 3600 ? $"{sec / 3600:F1} hr" :
        sec >= 60   ? $"{sec / 60:F1} min"  : $"{sec:F1} sec";

    private static string ShortenModel(string model)
    {
        var s = model.Replace("claude-", "", StringComparison.OrdinalIgnoreCase);
        var parts = s.Split('-');
        if (parts.Length > 0 && parts[^1].Length == 8 && long.TryParse(parts[^1], out _))
            s = string.Join("-", parts[..^1]);
        return s;
    }
}

// =================== 料金テーブル ===================
class ModelPricingTable
{
    private readonly Dictionary<string, ModelPrice> _table =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "claude-opus-4",     new(15.00m, 75.00m, 18.75m, 1.50m) },
            { "claude-sonnet-4",   new( 3.00m, 15.00m,  3.75m, 0.30m) },
            { "claude-sonnet-3-7", new( 3.00m, 15.00m,  3.75m, 0.30m) },
            { "claude-sonnet-3-5", new( 3.00m, 15.00m,  3.75m, 0.30m) },
            { "claude-haiku-3-5",  new( 0.80m,  4.00m,  1.00m, 0.08m) },
            { "claude-opus-3",     new(15.00m, 75.00m, 18.75m, 1.50m) },
            { "claude-sonnet-3",   new( 3.00m, 15.00m,  3.75m, 0.30m) },
            { "claude-haiku-3",    new( 0.25m,  1.25m,  0.30m, 0.03m) },
        };

    public ModelPrice Lookup(string model)
    {
        if (_table.TryGetValue(model, out var p)) return p;
        foreach (var (key, price) in _table)
            if (model.StartsWith(key, StringComparison.OrdinalIgnoreCase)) return price;
        return new ModelPrice(3.00m, 15.00m, 3.75m, 0.30m);
    }

    public decimal CalcCost(string model, long input, long output, long cacheRead, long cacheWrite)
    {
        var p = Lookup(model);
        return (input * p.InputPer1M + output * p.OutputPer1M
              + cacheWrite * p.CacheWritePer1M + cacheRead * p.CacheReadPer1M) / 1_000_000m;
    }
}

// =================== OTLP 属性ユーティリティ ===================
static class OtlpAttr
{
    public static string? GetString(JsonElement attrs, string key)
    {
        if (attrs.ValueKind != JsonValueKind.Array) return null;
        foreach (var attr in attrs.EnumerateArray())
        {
            if (!attr.TryGetProperty("key", out var k) || k.GetString() != key) continue;
            if (attr.TryGetProperty("value", out var v) && v.TryGetProperty("stringValue", out var sv))
                return sv.GetString();
        }
        return null;
    }

    public static long GetLong(JsonElement attrs, string key)
    {
        foreach (var attr in attrs.EnumerateArray())
        {
            if (!attr.TryGetProperty("key", out var k) || k.GetString() != key) continue;
            if (!attr.TryGetProperty("value", out var v)) continue;
            if (v.TryGetProperty("intValue", out var iv))
                return iv.ValueKind == JsonValueKind.String
                    ? long.TryParse(iv.GetString(), out var l) ? l : 0
                    : iv.GetInt64();
            if (v.TryGetProperty("doubleValue", out var dv)) return (long)dv.GetDouble();
        }
        return 0;
    }
}

// =================== 型定義 ===================
record ModelPrice(decimal InputPer1M, decimal OutputPer1M, decimal CacheWritePer1M, decimal CacheReadPer1M);

class ModelMetrics
{
    public long    Input      { get; set; }
    public long    Output     { get; set; }
    public long    CacheRead  { get; set; }
    public long    CacheWrite { get; set; }
    public decimal Cost       { get; set; }
}

class TraceTokens
{
    public long Input      { get; set; }
    public long Output     { get; set; }
    public long CacheRead  { get; set; }
    public long CacheWrite { get; set; }
}
