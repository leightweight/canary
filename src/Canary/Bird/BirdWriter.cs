using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Canary.Bird;

internal sealed class BirdWriter(
        Stream stream,
        Encoding encoding)
    : IDisposable, IAsyncDisposable
{
    private const string Hello = "0001 welcome";
    private const string AlreadyDisabled = "0008 {0}: already disabled";
    private const string Disabled = "0009 {0}: disabled";
    private const string AlreadyEnabled = "0010 {0}: already enabled";
    private const string Enabled = "0011 {0}: enabled";
    private const string NoProtocolsMatch = "8003 {0}: no protocols match";
    private const string ParseError = "9001 parse error";

    private readonly TextWriter _writer = new StreamWriter(stream, encoding);

    public Task WriteHelloAsync(CancellationToken cancellationToken)
        => WriteResponseAsync(Hello, cancellationToken);

    public Task WriteAlreadyDisabledAsync(string protocol, CancellationToken cancellationToken)
        => WriteResponseAsync(string.Format(AlreadyDisabled, protocol), cancellationToken);

    public Task WriteDisabledAsync(string protocol, CancellationToken cancellationToken)
        => WriteResponseAsync(string.Format(Disabled, protocol), cancellationToken);

    public Task WriteAlreadyEnabledAsync(string protocol, CancellationToken cancellationToken)
        => WriteResponseAsync(string.Format(AlreadyEnabled, protocol), cancellationToken);

    public Task WriteEnabledAsync(string protocol, CancellationToken cancellationToken)
        => WriteResponseAsync(string.Format(Enabled, protocol), cancellationToken);

    public Task WriteNoProtocolsMatchAsync(string protocol, CancellationToken cancellationToken)
        => WriteResponseAsync(string.Format(NoProtocolsMatch, protocol), cancellationToken);

    public Task WriteParseErrorAsync(CancellationToken cancellationToken)
        => WriteResponseAsync(ParseError, cancellationToken);

    private async Task WriteResponseAsync(
        string response,
        CancellationToken cancellationToken)
    {
        await _writer.WriteLineAsync(response);
        await _writer.FlushAsync(cancellationToken);
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
    }
}
