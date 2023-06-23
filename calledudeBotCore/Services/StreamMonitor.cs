using calledudeBot.Bots;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IStreamMonitor : INotificationHandler<ReadyNotification>
{
}

public sealed class StreamMonitor : IStreamMonitor
{
	private CancellationToken _cancellationToken;
	private readonly IOBSWebsocket _obs;
	private readonly IProcessMonitorService _processMonitorService;
	private readonly IDiscordUserWatcher _discordUserWatcher;
	private readonly IStreamingState _streamingState;
	private readonly SemaphoreSlim _exitSem;
	private readonly ILogger<StreamMonitor> _logger;
	private readonly AsyncAutoResetEvent _streamStartedEvent;

	public StreamMonitor(
		ILogger<StreamMonitor> logger,
		IOBSWebsocket obs,
		IProcessMonitorService processMonitorService,
		IDiscordUserWatcher discordUserWatcher,
		IStreamingState streamingState)
	{
		_streamStartedEvent = new(false);

		_exitSem = new SemaphoreSlim(1);

		_logger = logger;
		_obs = obs;
		_processMonitorService = processMonitorService;
		_discordUserWatcher = discordUserWatcher;
		_streamingState = streamingState;
	}

	public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
	{
		if (notification.Bot is DiscordBot)
		{
			_cancellationToken = cancellationToken;
			_obs.StreamStatus += OnStreamStatus;
			_obs.StreamingStateChanged += OnStreamingStateChanged;
			_obs.Disconnected += OnDisconnected;
			_ = Connect();
		}

		return Task.CompletedTask;
	}

	// TODO: Combine OnStreamingStateChanged and OnStreamStatus
	private void OnStreamingStateChanged(object? sender, StreamStateChangedEventArgs stateChange)
	{
		if (stateChange.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STARTED)
		{
			_logger.LogInformation("OBS stream has gone live.");
			_streamStartedEvent.Set();
		}
		else if (stateChange.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED)
		{
			_streamingState.IsStreaming = false;
			_logger.LogInformation("OBS stream has stopped.");
		}
	}

	private async void OnDisconnected(object? sender, ObsDisconnectionInfo disconnectionInfo)
	{
		// TODO: Not needed..?
		if (!await _exitSem.WaitAsync(150, _cancellationToken))
			return;

		_streamingState.IsStreaming = false;
		//await _streamStatusTimer.Stop();
		await _obs.Disconnect();

		await _processMonitorService.WaitForProcessesToQuit();
		await Connect();

		_exitSem.Release();
	}

	private void OnStreamStatus(object? sender, OutputStatus status)
	{
		if (status.IsActive == _streamingState.IsStreaming && _streamingState.StreamStarted != default)
			return;

		if (status.IsActive)
		{
			_streamingState.StreamStarted = DateTime.Now.AddMilliseconds(-status.Duration);
			//_streamStatusTimer.Start(CheckDiscordStatus, _cancellationToken);
		}
	}

	private async Task Connect()
	{
		await _processMonitorService.WaitForProcessToStart("obs64", "obs32");

		if (!await TryConnect())
		{
			_logger.LogWarning("You need to install the obs-websocket plugin for OBS and configure it to run on port 4444.");
			_logger.LogWarning("Go to this URL to download it: {downloadUrl}", "https://github.com/Palakis/obs-websocket/releases");
			return;
		}

		_logger.LogInformation("Connected to OBS. Start streaming!");

		// TODO: "Deadlock" can technically happen here if OBS exits at this stage
		await _streamStartedEvent.WaitAsync(_cancellationToken);

		if (!await _discordUserWatcher.TryWaitForUserStreamToStart())
			return;

		_logger.LogInformation("Stream started.");
	}

	private async Task<bool> TryConnect()
	{
		//Trying 5 times just in case.
		var connectionFailed = await Enumerable.Range(0, 5)
			.ToAsyncEnumerable()
			.SelectAwait(async _ => await _obs.TryConnect())
			.AllAsync(success => !success);

		return !connectionFailed;
	}
}
