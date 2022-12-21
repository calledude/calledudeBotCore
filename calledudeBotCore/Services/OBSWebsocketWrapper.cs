using calledudeBot.Config;
using calledudeBot.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IOBSWebsocket
{
	//event StreamStatusCallback StreamStatus;
	event EventHandler<StreamStateChangedEventArgs> StreamingStateChanged;
	event EventHandler<ObsDisconnectionInfo> Disconnected;
	event EventHandler Connected;

	bool TryConnect();
	void Disconnect();
}

public class OBSWebsocketWrapper : IOBSWebsocket
{
	//public event StreamStatusCallback StreamStatus
	//{
	//	add => _obs.StreamStatus += value;
	//	remove => _obs.StreamStatus -= value;
	//}

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

	public event EventHandler Connected
	{
		add => _obs.Connected += value;
		remove => _obs.Connected -= value;
	}

	private readonly string? _websocketUrl;
	private readonly int? _websocketPort;
	private readonly ILogger<OBSWebsocketWrapper> _logger;
	private readonly IAsyncTimer _timer;
	private readonly OBSWebsocket _obs;
	private readonly AutoResetEvent _connected;

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

		_connected = new AutoResetEvent(false);

		_timer = timer;
		_timer.Interval = 2000;
	}

	public bool TryConnect()
	{
		if (_websocketUrl is null || _websocketPort is null)
		{
			_logger.LogWarning("Invalid OBS websocket URL or port. Bailing out.");
			return false;
		}

		_obs.ConnectAsync($"ws://{_websocketUrl}:{_websocketPort}", null);
		_connected.WaitOne();

		if (_obs.IsConnected)
		{
			//_obs.wsConnection.Log.Output = static (_, __) => { };
		}

		_timer.Start(CheckStreamStatus, CancellationToken.None);
		return _obs.IsConnected;
	}

	private void OnConnected(object? sender, EventArgs e) => _connected.Set();

	private Task CheckStreamStatus(CancellationToken arg)
	{
		var status = _obs.GetStreamStatus();

		return Task.CompletedTask;
	}

	public void Disconnect() => _obs.Disconnect();
}
