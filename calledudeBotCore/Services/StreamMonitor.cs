using calledudeBot.Bots;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Utilities;
using Discord;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IStreamMonitor : INotificationHandler<ReadyNotification>, IDisposable
{
}

public sealed class StreamMonitor : IStreamMonitor
{
	private IGuildUser? _streamer;
	private ITextChannel? _announceChannel;
	private CancellationToken _cancellationToken;
	private readonly IOBSWebsocket _obs;
	private readonly IProcessMonitorService _processMonitorService;
	private readonly IAsyncTimer _streamStatusTimer;
	private readonly IStreamingState _streamingState;
	private readonly SemaphoreSlim _exitSem;
	private readonly ILogger<StreamMonitor> _logger;
	private readonly IDiscordSocketClient _client;
	private readonly ulong _announceChannelID;
	private readonly ulong _streamerID;

	public StreamMonitor(
		ILogger<StreamMonitor> logger,
		IOptions<BotConfig> options,
		IDiscordSocketClient client,
		IOBSWebsocket obs,
		IProcessMonitorService processMonitorService,
		IAsyncTimer timer,
		IStreamingState streamingState)
	{
		_exitSem = new SemaphoreSlim(1);

		_logger = logger;
		_client = client;

		_obs = obs;
		_processMonitorService = processMonitorService;

		_streamStatusTimer = timer;
		_streamStatusTimer.Interval = 2000;

		_streamingState = streamingState;

		var config = options.Value;
		_announceChannelID = config.AnnounceChannelId;
		_streamerID = config.StreamerId;
	}

	public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
	{
		if (notification.Bot is DiscordBot)
		{
			_cancellationToken = cancellationToken;
			//_obs.StreamStatus += OnStreamStatus;
			_obs.StreamingStateChanged += OnStreamingStateChanged;
			_obs.Connected += OnConnected;
			_obs.Disconnected += OnDisconnected;
			_ = Connect();
		}

		return Task.CompletedTask;
	}

	private void OnStreamingStateChanged(object? sender, StreamStateChangedEventArgs stateChange)
	{
		if (stateChange.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STARTED)
		{
			_logger.LogInformation("OBS has gone live. Checking discord status for user {discordUserName}#{discriminator}..", _streamer!.Username, _streamer.Discriminator);
			_streamStatusTimer.Start(CheckDiscordStatus, _cancellationToken);
		}
		else if (stateChange.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED)
		{
			_streamingState.IsStreaming = false;
			_streamStatusTimer.Stop();
			_logger.LogInformation("Stream stopped.");
		}
	}

	private void OnConnected(object? sender, EventArgs e)
		=> _logger.LogInformation("Connected to OBS. Start streaming!");

	private async void OnDisconnected(object? sender, ObsDisconnectionInfo disconnectionInfo)
	{
		if (!await _exitSem.WaitAsync(150, _cancellationToken))
			return;

		_streamingState.IsStreaming = false;
		await _streamStatusTimer.Stop();
		_obs.Disconnect();

		await _processMonitorService.WaitForProcessesToQuit();
		await Connect();

		_exitSem.Release();
	}

	//private void OnStreamStatus(object? sender, StreamStatus status)
	//{
	//	if (status.Streaming == _streamingState.IsStreaming && _streamingState.StreamStarted != default)
	//		return;

	//	if (status.Streaming)
	//	{
	//		_streamingState.StreamStarted = DateTime.Now.AddSeconds(-status.TotalStreamTime);
	//		_streamStatusTimer.Start(_cancellationToken);
	//	}
	//}

	private async Task Connect()
	{
		_announceChannel = _client.GetMessageChannel(_announceChannelID) as ITextChannel;
		if (_announceChannel is null)
		{
			_logger.LogWarning("Invalid channel. Will not announce when stream goes live.");
			return;
		}

		_streamer = await _announceChannel.Guild.GetUserAsync(_streamerID);
		if (_streamer is null)
		{
			_logger.LogWarning("Invalid StreamerID. Could not find user.");
			return;
		}

		await _processMonitorService.WaitForProcessToStart("obs64", "obs32");
		TryConnect();
	}

	private void TryConnect()
	{
		//Trying 5 times just in case.
		var connectionFailed = Enumerable.Range(0, 5)
			.Select(_ => _obs.TryConnect())
			.All(success => !success);

		if (connectionFailed)
		{
			_logger.LogWarning("You need to install the obs-websocket plugin for OBS and configure it to run on port 4444.");
			_logger.LogWarning("Go to this URL to download it: {downloadUrl}", "https://github.com/Palakis/obs-websocket/releases");
		}
	}

	private async Task CheckDiscordStatus(CancellationToken cancellationToken)
	{
		if (_streamer?.Activities.FirstOrDefault(x => x is StreamingGame) is not StreamingGame activity)
			return;

		await _streamStatusTimer.Stop();
		_streamingState.IsStreaming = true;

		var messages = await _announceChannel!
			.GetMessagesAsync()
			.FlattenAsync();

		if (!CurrentStreamHasBeenAnnounced(messages, activity, out var announcementMessage))
		{
			_logger.LogInformation("Streamer went live. Sending announcement to #{channelName}", _announceChannel.Name);
			await _announceChannel.SendMessageAsync(announcementMessage);
			_streamingState.SessionId = Guid.NewGuid();
		}
	}

	//StreamStarted returns the _true_ time that the stream started
	//If any announcement message exists within 3 minutes of that, don't send a new announcement
	//In that case we assume that the bot has been restarted (for whatever reason)
	private bool CurrentStreamHasBeenAnnounced(IEnumerable<IMessage> messages, StreamingGame activity, out string announcementMessage)
	{
		var twitchUsername = activity.Url.Split('/').Last();
		var msg = $"🔴 **{twitchUsername}** is now **LIVE**\n- Title: **{activity.Details}**\n- Watch at: {activity.Url}";
		announcementMessage = msg;
		return messages.Any(m =>
						m.Author.Id == _client.CurrentUser.Id
						&& m.Content.Equals(msg)
						&& _streamingState.StreamStarted - m.Timestamp < TimeSpan.FromMinutes(3));
	}

	public void Dispose() => _obs.Disconnect();
}
