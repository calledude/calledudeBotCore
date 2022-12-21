using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Utilities;

public interface IAsyncTimer
{
	int Interval { get; set; }

	void Start(Func<CancellationToken, Task> callback, CancellationToken cancellationToken);
	Task Stop();
}

public sealed class AsyncTimer : IAsyncTimer
{
	private readonly CancellationTokenSource _cts;
	private readonly ILogger<AsyncTimer> _logger;
	private bool _started;
	private PeriodicTimer? _periodicTimer;
	private Task? _workTask;

	public int Interval { get; set; }

	public AsyncTimer(ILogger<AsyncTimer> logger)
	{
		_cts = new CancellationTokenSource();
		_logger = logger;
	}

	public void Start(Func<CancellationToken, Task> callback, CancellationToken cancellationToken)
	{
		if (Interval <= 0)
			throw new ArgumentOutOfRangeException(nameof(Interval));

		if (_started)
			return;

		_started = true;
		var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

		_periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(Interval));
		_workTask = Tick(callback, linkedTokenSource);
	}

	private async Task Tick(Func<CancellationToken, Task> callback, CancellationTokenSource cancellationTokenSource)
	{
		_logger.LogTrace("Starting timer.");
		while (await _periodicTimer!.WaitForNextTickAsync(cancellationTokenSource.Token) && !cancellationTokenSource.IsCancellationRequested)
		{
			await callback.Invoke(cancellationTokenSource.Token);
		}

		_logger.LogTrace("Stopping timer.");
	}

	public async Task Stop()
	{
		if (_workTask is null || !_started)
			return;

		_cts.Cancel();
		await _workTask;
		_cts.Dispose();
		_workTask.Dispose();
		_periodicTimer!.Dispose();
	}
}