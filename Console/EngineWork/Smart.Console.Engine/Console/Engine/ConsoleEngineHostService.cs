using System.Threading;
using System.Threading.Tasks;

namespace Smart.Console.Engine
{
    using Microsoft.Extensions.Hosting;

    public class ConsoleEngineHostService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
