using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.MiddlewareAnalysis;
using Microsoft.AspNetCore.ResponseCompression;

using Prometheus;
using Serilog;

using System.Diagnostics;
using System.IO.Compression;
using System.Net.Mime;

using WorkPipeline.Infrastructure;

//----------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(option =>
{
    option.ReadFrom.Configuration(builder.Configuration);
});
builder.Services.AddHttpLogging(options =>
{
    //options.LoggingFields = HttpLoggingFields.All;
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.RequestQuery |
                            HttpLoggingFields.ResponsePropertiesAndHeaders;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// API
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName);
    };
});

// SignalR
builder.Services.AddSignalR();

// Compress
builder.Services.AddRequestDecompression();
builder.Services.AddResponseCompression(options =>
{
    // Default false (for CRIME and BREACH attacks)
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = new[] { MediaTypeNames.Application.Json };
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

// Rate limit
builder.Services.AddRateLimiter(_ =>
{
});

// Health
builder.Services.AddHealthChecks();

// Develop
if (!builder.Environment.IsProduction())
{
    // Profiler
    builder.Services.AddMiniProfiler(options =>
    {
        options.RouteBasePath = "/profiler";
    });

    // Swagger
    builder.Services.AddSwaggerGen();
}

// *
builder.Services.AddMiddlewareAnalysis();
builder.Services.Insert(0, ServiceDescriptor.Transient<IStartupFilter, AnalysisStartupFilter>());

//----------------------------------------

var app = builder.Build();

// *
var listener = app.Services.GetRequiredService<DiagnosticListener>();
var observer = ActivatorUtilities.CreateInstance<AnalysisDiagnosticAdapter>(app.Services);
using var disposable = listener.SubscribeWithAdapter(observer);

//----------------------------------------

// Configure the HTTP request pipeline.

// Serilog
if (app.Environment.IsDevelopment())
{
    app.UseSerilogRequestLogging(options =>
    {
        options.IncludeQueryInRequestPath = true;
    });
}

// HTTP log
if (!app.Environment.IsProduction())
{
    // Profiler
    app.UseMiniProfiler();

//    app.UseWhen(
//        c => c.Request.Path.StartsWithSegments("/api"),
//        b => b.UseHttpLogging());
    app.UseHttpLogging();
}

// Forwarded headers
app.UseForwardedHeaders();

//if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    //app.UseExceptionHandler();
    // TODO
    app.UseWhen(c => c.Request.Path.StartsWithSegments("/api"), b => b.UseExceptionHandler());
    app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api"), b => b.UseExceptionHandler("/Home/Error"));
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// app.UseCookiePolicy();

app.UseRouting();
app.UseRateLimiter();
// app.UseRequestLocalization();

// app.UseCors();

if (app.Environment.IsDevelopment())
{
    // Profiler
    app.UseMiniProfiler();

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health
app.UseHealthChecks("/health");

// Metrics
app.UseHttpMetrics();

// Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// app.UseSession();
// TODO + mapwhen
app.UseResponseCompression();
app.UseRequestDecompression();
// app.UseResponseCaching();

//----------------------------------------

// Map
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR

// Health
app.MapHealthChecks("/health");

// Metrics
app.MapMetrics();

app.Run();
