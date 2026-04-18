using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetModulith.Api.HealthChecks;

internal sealed class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;

    public TcpHealthCheck(string host, int port, TimeSpan? timeout = null)
    {
        _host = host;
        _port = port;
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            await client.ConnectAsync(_host, _port, timeoutCts.Token);

            return HealthCheckResult.Healthy($"TCP endpoint {_host}:{_port} is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: $"TCP endpoint {_host}:{_port} is not reachable.",
                exception: ex);
        }
    }

    public static (string Host, int Port) ParseEndpoint(string rawEndpoint, int defaultPort)
    {
        if (string.IsNullOrWhiteSpace(rawEndpoint))
            return ("localhost", defaultPort);

        var endpoint = rawEndpoint.Split(',', 2)[0].Trim();

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            !string.IsNullOrWhiteSpace(uri.Host))
        {
            return (uri.Host, uri.Port > 0 ? uri.Port : defaultPort);
        }

        var parts = endpoint.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], out var parsedPort))
        {
            return (parts[0], parsedPort);
        }

        return (endpoint, defaultPort);
    }
}
