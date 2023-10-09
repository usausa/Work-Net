using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.MiddlewareAnalysis;

using Serilog;

using System.Diagnostics;

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

// Swagger
builder.Services.AddSwaggerGen();

// Health
builder.Services.AddHealthChecks();

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
if (app.Environment.IsDevelopment())
{
//    app.UseWhen(
//        c => c.Request.Path.StartsWithSegments("/api"),
//        b => b.UseHttpLogging());
    app.UseHttpLogging();
}

// TODO
//if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    //app.UseExceptionHandler();
    app.MapWhen(c => c.Request.Path.StartsWithSegments("/api"), b => b.UseExceptionHandler());
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// app.UseCookiePolicy();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
// app.UseRateLimiter();
// app.UseRequestLocalization();
// app.UseCors();

//app.UseAuthentication();
app.UseAuthorization();

// app.UseSession();
// app.UseResponseCompression();
// app.UseResponseCaching();

//----------------------------------------

// Map
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

app.Run();
