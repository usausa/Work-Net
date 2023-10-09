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

// Add services to the container.
builder.Services.AddControllersWithViews();

// API
builder.Services.AddEndpointsApiExplorer();

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

if (!app.Environment.IsDevelopment())
{
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
