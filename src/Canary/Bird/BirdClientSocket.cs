using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Serilog.Context;

namespace Canary.Bird;

internal sealed class BirdClientSocket(
    CanaryContext context,
    Socket socket)
{
    public async Task HandleAsync()
    {
        var id = Convert.ToBase64String(RandomNumberGenerator.GetBytes(10))[..6];
        using var idContext = LogContext.PushProperty("Client", id);

        await using var stream = new NetworkStream(socket, true);
        await using var writer = new BirdWriter(stream, Encoding.ASCII);
        using var reader = new StreamReader(stream, Encoding.ASCII);

        await writer.WriteHelloAsync(context.StoppingToken);
        await HandleCommandsAsync(writer, reader);
    }

    private async Task HandleCommandsAsync(
        BirdWriter writer,
        TextReader reader)
    {
        try
        {
            context.Log.Debug("Handling connection from client");

            while (!context.StoppingToken.IsCancellationRequested)
            {
                try
                {
                    var line = await reader.ReadLineAsync(context.StoppingToken);
                    if (line is null)
                        continue;

                    var command = BirdCommand.Parse(line);

                    switch (command)
                    {
                        case DisableProtocolBirdCommand d:
                            await HandleDisableAsync(writer, d);
                            break;

                        case EnableProtocolBirdCommand e:
                            await HandleEnableAsync(writer, e);
                            break;

                        default:
                            context.Log.Warning("Received unknown command {Line}", command.Line);
                            await writer.WriteParseErrorAsync(context.StoppingToken);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }
        }
        catch (Exception e)
        {
            context.Log.Error(e, "Exception handling commands");
            throw;
        }
        finally
        {
            context.Log.Information("Finished handling connection");
            socket.Dispose();
        }
    }

    private async Task HandleDisableAsync(
        BirdWriter writer,
        DisableProtocolBirdCommand command)
    {
        var protocol = context.BirdProtocols
            .Find(p => p.Name == command.Protocol);

        if (protocol is null)
        {
            await writer.WriteNoProtocolsMatchAsync(command.Protocol, context.StoppingToken);
            context.Log.Warning("Client attempted to disable unknown protocol {Protocol}", command.Protocol);
            return;
        }

        if (!protocol.IsEnabled)
        {
            await writer.WriteAlreadyDisabledAsync(protocol.Name, context.StoppingToken);
            context.Log.Information("Ignoring attempt to disable protocol {Protocol}", protocol.Name);
            return;
        }

        protocol.IsEnabled = false;
        await writer.WriteDisabledAsync(protocol.Name, context.StoppingToken);

        context.Log.Information("Disabled protocol {Protocol}", protocol.Name);
    }

    private async Task HandleEnableAsync(
        BirdWriter writer,
        EnableProtocolBirdCommand command)
    {
        var protocol = context.BirdProtocols
            .Find(p => p.Name == command.Protocol);

        if (protocol is null)
        {
            await writer.WriteNoProtocolsMatchAsync(command.Protocol, context.StoppingToken);
            context.Log.Warning("Client attempted to enable unknown protocol {Protocol}", command.Protocol);
            return;
        }

        if (protocol.IsEnabled)
        {
            await writer.WriteAlreadyEnabledAsync(protocol.Name, context.StoppingToken);
            context.Log.Information("Ignoring attempt to enable protocol {Protocol}", protocol.Name);
            return;
        }

        protocol.IsEnabled = true;
        await writer.WriteEnabledAsync(protocol.Name, context.StoppingToken);

        context.Log.Information("Enabled protocol {Protocol}", protocol.Name);
    }
}
