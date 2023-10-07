using Microsoft.AspNetCore.MiddlewareAnalysis;

using System.Diagnostics;

using WorkPipeline.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// *
builder.Services.AddMiddlewareAnalysis();
builder.Services.Insert(0, ServiceDescriptor.Transient<IStartupFilter, AnalysisStartupFilter>());

var app = builder.Build();

// *
var listener = app.Services.GetRequiredService<DiagnosticListener>();
var observer = ActivatorUtilities.CreateInstance<AnalysisDiagnosticAdapter>(app.Services);
using var disposable = listener.SubscribeWithAdapter(observer);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
