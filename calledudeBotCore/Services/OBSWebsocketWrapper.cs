using calledudeBot.Config;
using calledudeBot.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IOBSWebsocket
{
	event EventHandler<OutputStatus> StreamStatus;
	event EventHandler<StreamStateChangedEventArgs> StreamingStateChanged;
	event EventHandler<ObsDisconnectionInfo> Disconnected;

	Task<bool> TryConnect();
	Task Disconnect();
}

public class OBSWebsocketWrapper : IOBSWebsocket
{
	public event EventHandler<OutputStatus>? StreamStatus;

	public event EventHandler<StreamStateChangedEventArgs> StreamingStateChanged
	{
		add => _obs.StreamStateChanged += value;
		remove => _obs.StreamStateChanged -= value;
	}

	public event EventHandler<ObsDisconnectionInfo> Disconnected
	{
		add => _obs.Disconnected += value;
		remove => _obs.Disconnected -= value;
	}

	private readonly string? _websocketUrl;
	private readonly int? _websocketPort;
	private readonly ILogger<OBSWebsocketWrapper> _logger;
	private readonly IAsyncTimer _timer;
	private readonly OBSWebsocket _obs;
	private readonly AsyncAutoResetEvent _connected;

	public OBSWebsocketWrapper(
		OBSWebsocket obs,
		ILogger<OBSWebsocketWrapper> logger,
		IOptions<BotConfig> options,
		IAsyncTimer timer)
	{
		var config = options.Value;
		_websocketUrl = config.OBSWebsocketUrl;
		_websocketPort = config.OBSWebsocketPort;

		_logger = logger;
		_obs = obs;
		_obs.Connected += OnConnected;

		_connected = new AsyncAutoResetEvent(false);

		_timer = timer;
	}

	public async Task<bool> TryConnect()
	{
		if (_websocketUrl is null || _websocketPort is null)
		{
			_logger.LogWarning("Invalid OBS websocket URL or port. Bailing out.");
			return false;
		}

		_obs.ConnectAsync($"ws://{_websocketUrl}:{_websocketPort}", null);
		var timeoutTask = Task.Delay(1000);
		var connectionTask = _connected.WaitAsync();

		var completedTask = await Task.WhenAny(timeoutTask, connectionTask);
		if (completedTask == timeoutTask)
		{
			_logger.LogWarning("Attempt to connect to OBS timed out.");
			return false;
		}

		_timer.Start(CheckStreamStatus, 2000, CancellationToken.None);

		return _obs.IsConnected;
	}

	private void OnConnected(object? sender, EventArgs e) => _connected.Set();

	private Task CheckStreamStatus(CancellationToken arg)
	{
		if (!_obs.IsConnected)
			return Task.CompletedTask;

		var status = _obs.GetStreamStatus();
		StreamStatus?.Invoke(this, status);

		return Task.CompletedTask;
	}

	public async Task Disconnect()
	{
		await _timer.Stop();
		_obs.Disconnect();
	}
}