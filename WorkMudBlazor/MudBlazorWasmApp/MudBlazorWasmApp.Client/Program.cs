using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

namespace MudBlazorWasmApp.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddMudServices();

            await builder.Build().RunAsync();
        }
    }
}
