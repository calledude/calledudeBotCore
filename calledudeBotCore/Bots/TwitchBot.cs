﻿using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public interface ITwitchBot : IMessageBot<IrcMessage>;

public sealed class TwitchBot : TwitchBotBase, ITwitchBot
{
	private HashSet<string>? _mods;
	private DateTime _lastModCheck;
	private readonly IMessageDispatcher _dispatcher;
	private readonly ILogger<TwitchBot> _logger;
	private readonly Channel<string[]> _modsChan;
	private readonly string _moderatorMessage;

	public override string Name => "TwitchBot";

	private static readonly char[] _modListSeparator = [',', ' '];

	public TwitchBot(
		IIrcClient ircClient,
		ITwitchBotConfig config,
		IMessageDispatcher dispatcher,
		ILogger<TwitchBot> logger) : base(ircClient, config)
	{
		_logger = logger;
		IrcClient.Ready += OnReady;
		IrcClient.MessageReceived += HandleMessage;
		IrcClient.UnhandledMessage += HandleRawMessage;
		IrcClient.ChatUserJoined += OnChatUserJoin;
		IrcClient.ChatUserLeft += OnChatUserLeave;

		_dispatcher = dispatcher;
		_modsChan = Channel.CreateBounded<string[]>(1);

		_moderatorMessage = $":tmi.twitch.tv NOTICE {ChannelName} :The moderators of this channel are:";
	}

	public async Task OnReady()
	{
		await IrcClient.WriteLine("CAP REQ :twitch.tv/commands");
		await IrcClient.WriteLine("CAP REQ :twitch.tv/membership");
		await _dispatcher.PublishAsync(new ReadyNotification(this), CancellationToken);
	}

	public async Task HandleMessage(string message, string user)
	{
		var msg = new IrcMessage
		{
			Content = message,
			Channel = ChannelName,
			Sender = new User(user, () => IsMod(user))
		};

		var msgPublishTask = _dispatcher.PublishAsync(msg);
		var relayPublishTask = _dispatcher.PublishAsync(new RelayNotification<IrcMessage>(this, msg));

		await Task.WhenAll(msgPublishTask, relayPublishTask);
	}

	private async Task<bool> IsMod(string user)
	{
		if (IsBroadcaster(user))
			return true;

		var mods = await GetMods();
		return mods.Contains(user);
	}

	private bool IsBroadcaster(string user)
		=> user.Equals(Broadcaster, StringComparison.OrdinalIgnoreCase);

	public async Task HandleRawMessage(string buffer)
	{
		// :tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: CALLEDUDE, calLEDuDeBoT
		if (!buffer.StartsWith(_moderatorMessage))
			return;

		var modsIndex = _moderatorMessage.Length;
		var modsArr = buffer[modsIndex..].Split(_modListSeparator, StringSplitOptions.RemoveEmptyEntries);

		await _modsChan.Writer.WriteAsync(modsArr);
	}

	private async Task OnChatUserLeave(string user)
		=> await HandleUserParticipation(user, ParticipationType.Leave, "left");

	private async Task OnChatUserJoin(string user)
		=> await HandleUserParticipation(user, ParticipationType.Join, "joined");

	public async Task HandleUserParticipation(string user, ParticipationType participationType, string logUserAction)
	{
		_logger.LogInformation("User: {userName} {userAction} the chat.", user, logUserAction);
		await _dispatcher.PublishAsync(new UserParticipationNotification(new User(user), participationType));
	}

	public async Task<HashSet<string>> GetMods()
	{
		if (DateTime.UtcNow <= _lastModCheck.AddMinutes(1))
			return _mods!;

		_logger.LogInformation("Getting moderators");
		_lastModCheck = DateTime.UtcNow;

		await IrcClient.WriteLine($"PRIVMSG {ChannelName} /mods");
		var modsArr = await _modsChan.Reader.ReadAsync();
		_mods = modsArr.ToHashSet(StringComparer.OrdinalIgnoreCase);

		_logger.LogInformation("Fetched moderators: {moderators}", _mods);

		return _mods;
	}
}