using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots.Network;

public interface ITcpClient : IDisposable
{
    string? Server { get; set; }
    int Port { get; set; }
    bool Connected { get; }
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
    Task WriteLineAsync(string value);

    void Close();
    Stream GetStream();
    Task Reconnect();
    Task Setup();
}

public sealed class TcpClientAdapter : ITcpClient
{
    private TcpClient _tcpClient;
    private readonly ILogger<TcpClient> _logger;

    public bool Connected => _tcpClient.Connected;

    public string? Server { get; set; }
    public int Port { get; set; }

    private StreamWriter? OutputStream { get; set; }
    private StreamReader? InputStream { get; set; }

    public TcpClientAdapter(ILogger<TcpClient> logger)
    {
        _tcpClient = new TcpClient();
        _logger = logger;
    }

    public async Task Reconnect()
    {
        _logger.LogWarning("Disconnected. Re-establishing connection..");
        Dispose();

        for (var retryMultiplier = 1; !_tcpClient.Connected; retryMultiplier *= 2)
        {
            try
            {
                Dispose();
                await Setup();

                var nextRetryInMs = 5000 * retryMultiplier;
                _logger.LogInformation("Retrying in {nextRetryMs} seconds. Retries made: {retriesMade}", nextRetryInMs / 1000, (retryMultiplier / 2) + 1);
                await Task.Delay(nextRetryInMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured when trying to reconnect");
            }
        }
    }

    public async Task Setup()
    {
        if (Server is null)
            throw new InvalidOperationException($"{nameof(Server)} needs to be set before running {nameof(Setup)}");

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(Server, Port);

        OutputStream = new StreamWriter(_tcpClient.GetStream())
        {
            AutoFlush = true
        };

        InputStream = new StreamReader(_tcpClient.GetStream());
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (InputStream is null)
            return null;

        return await InputStream.ReadLineAsync().WaitAsync(cancellationToken);
    }

    public async Task WriteLineAsync(string value)
    {
        if (OutputStream is null)
            return;

        await OutputStream.WriteLineAsync(value);
    }

    public Stream GetStream()
        => _tcpClient.GetStream();

    public void Close()
        => _tcpClient.Close();

    public void Dispose()
    {
        _tcpClient.Dispose();
        OutputStream?.Dispose();
        InputStream?.Dispose();
    }
}
