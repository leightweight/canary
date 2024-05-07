using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog.Context;

namespace Canary.Bird;

internal class BirdServer(
    CanaryContext context)
{
    public async Task RunAsync()
    {
        using var serverContext = LogContext.PushProperty("Server", "Bird");

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        socket.Bind(new UnixDomainSocketEndPoint(context.BirdSocketPath));
        socket.Listen();

        try
        {
            context.Log.Information(
                "Listening for connections on {Path}",
                context.BirdSocketPath);

            while (!context.StoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await socket.AcceptAsync(context.StoppingToken);
                    var handler = new BirdClientSocket(context, client);
                    _ = handler.HandleAsync();
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }

            context.Log.Information("Received stop signal");
        }
        finally
        {
            try
            {
                File.Delete(context.BirdSocketPath);
            }
            catch (FileNotFoundException)
            {
                // ignore
            }
            catch (DirectoryNotFoundException)
            {
                // ignore
            }
        }
    }
}
