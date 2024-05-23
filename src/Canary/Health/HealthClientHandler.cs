using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog.Events;

namespace Canary.Health;

internal class HealthClientHandler(
    CanaryContext context,
    HttpListenerContext httpContext)
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Task HandleAsync()
    {
        var path = httpContext.Request.Url?.AbsolutePath;
        if (path is null)
            return Task.CompletedTask;

        if (!string.Equals(httpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            return WriteNotFoundAsync();

        if (path == "/favicon.ico")
            return WriteNotFoundAsync();

        if (path == "/")
            return HandleIndexAsync();

        if (!path.StartsWith("/protocol/"))
            return WriteNotFoundAsync();

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries) is { Length: 2 } split
            ? HandleProtocolAsync(split[1])
            : WriteNotFoundAsync();
    }

    private Task HandleIndexAsync()
    {
        return WriteResponseAsync(
            200,
            new
            {
                Metadata = new
                {
                    ServerIp = httpContext.Request.LocalEndPoint.Address.ToString(),
                    ServerPort = httpContext.Request.LocalEndPoint.Port,
                    ClientIp = httpContext.Request.RemoteEndPoint.Address.ToString(),
                    ClientPort = httpContext.Request.RemoteEndPoint.Port
                },
                Protocols = context.BirdProtocols
            });
    }

    private Task HandleProtocolAsync(
        string name)
    {
        var protocol = context.BirdProtocols
            .Find(p => p.Name == name);
        if (protocol is null)
            return WriteNotFoundAsync();

        return WriteResponseAsync(
            protocol.IsEnabled
                ? 200
                : 500,
            protocol);
    }

    private Task WriteNotFoundAsync()
        => WriteResponseAsync(404, new { Error = "Resource not found" });

    private async Task WriteResponseAsync<T>(
        int status,
        T body)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.StatusCode = status;

        await JsonSerializer.SerializeAsync(
            httpContext.Response.OutputStream,
            body,
            _options,
            context.StoppingToken);
        httpContext.Response.OutputStream.Close();

        var level = status switch
        {
            404 => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        context.Log.Write(
            level,
            "{Method} {Path} responded {StatusCode}",
            httpContext.Request.HttpMethod,
            httpContext.Request.Url!.AbsolutePath,
            status);
    }
}
