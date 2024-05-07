using System;
using System.Threading;
using System.Threading.Tasks;
using Canary;
using Canary.Bird;
using Canary.Health;

using var tokenSource = new CancellationTokenSource();
await RunAsync(args, tokenSource);

return;

async Task RunAsync(string[] args, CancellationTokenSource stop)
{
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        stop.Cancel();
    };
    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    {
        stop.Cancel();
    };

    await using var context = CanaryContext.Create(args, stop.Token);

    var bird = new BirdServer(context);
    var health = new HealthServer(context);

    var tasks = new[]
    {
        bird.RunAsync(),
        health.RunAsync()
    };

    var complete = await Task.WhenAny(tasks);

    if (!complete.IsCompletedSuccessfully)
        await stop.CancelAsync();

    await Task.WhenAll(tasks);
}
