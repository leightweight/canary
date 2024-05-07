using System;
using System.Net;
using System.Threading.Tasks;
using Serilog.Context;

namespace Canary.Health;

internal class HealthServer(
    CanaryContext context)
{
    public async Task RunAsync()
    {
        using var serverContext = LogContext.PushProperty("Server", "Health");

        using var listener = new HttpListener();

        listener.Prefixes.Add(context.HealthEndpoint);
        listener.Start();

        context.Log.Information(
            "Listening for health checks on {Endpoint}",
            context.HealthEndpoint);

        while (!context.StoppingToken.IsCancellationRequested)
        {
            try
            {
                var httpContext = await listener
                    .GetContextAsync()
                    .WaitAsync(context.StoppingToken);

                var handler = new HealthClientHandler(context, httpContext);
                _ = handler.HandleAsync();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        context.Log.Information("Received stop signal");
    }
}
