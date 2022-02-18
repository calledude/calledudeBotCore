using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Utilities;

public interface IAsyncTimer : IDisposable
{
    event Func<CancellationToken, Task> Elapsed;
    int Interval { get; set; }

    Task Start(CancellationToken cancellationToken);
    void Stop();
}

public sealed class AsyncTimer : IAsyncTimer
{
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<AsyncTimer> _logger;

    public event Func<CancellationToken, Task>? Elapsed;
    public int Interval { get; set; }

    public AsyncTimer(ILogger<AsyncTimer> logger)
    {
        _cts = new CancellationTokenSource();
        _logger = logger;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        if (Elapsed is null)
            throw new InvalidOperationException(nameof(Elapsed) + " is null");

        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        _logger.LogTrace("Starting timer.");
        while (!linkedTokenSource.IsCancellationRequested)
        {
            await Elapsed(linkedTokenSource.Token);
            await Task.Delay(Interval, linkedTokenSource.Token);
        }

        _logger.LogTrace("Stopping timer.");
    }

    public void Stop() => _cts.Cancel();

    public void Dispose() => _cts.Dispose();
}
