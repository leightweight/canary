using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Canary;

internal sealed class CanaryContext : IDisposable, IAsyncDisposable
{
    public Logger Log { get; }
    public CancellationToken StoppingToken { get; }

    public string BirdSocketPath { get; }
    public List<BirdProtocol> BirdProtocols { get; }

    public string HealthEndpoint { get; }

    private CanaryContext(
        string birdSocketPath,
        string healthEndpoint,
        CancellationToken stoppingToken)
    {
        Log = InitializeLogger();
        StoppingToken = stoppingToken;
        BirdSocketPath = birdSocketPath;
        BirdProtocols = InitializeBirdProtocols();
        HealthEndpoint = healthEndpoint;
    }

    public static CanaryContext Create(
        string[] args,
        CancellationToken stoppingToken)
    {
        var healthEndpoint = args.Length >= 1
            ? $"http://+:{args[0]}/"
            : "http://+:80/";

        var birdSocketPath = args.Length >= 2
            ? args[1]
            : Path.Join(Directory.GetCurrentDirectory(), "canary.ctl");

        return new CanaryContext(
            birdSocketPath,
            healthEndpoint,
            stoppingToken);
    }

    private static Logger InitializeLogger()
    {
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                standardErrorFromLevel: LogEventLevel.Warning)
            .CreateLogger();
    }

    private static List<BirdProtocol> InitializeBirdProtocols()
    {
        return [new BirdProtocol("tailscale")];
    }

    public void Dispose()
    {
        Log.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Log.DisposeAsync();
    }
}

internal class BirdProtocol(string name)
{
    public string Name { get; } = name;
    public bool IsEnabled { get; set; } = true;
}
