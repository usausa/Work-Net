using DashboardContract;

using DashboardServer.Api;
using DashboardServer.Components;

using MudBlazor.Services;

using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// gRPC
builder.Services.AddCodeFirstGrpc();
builder.Services.AddSingleton<IDataApi, DataApi>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGrpcService<IDataApi>();

app.Run();
