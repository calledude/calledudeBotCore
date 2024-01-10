using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public sealed class MessageRelayService : INotificationHandler<RelayNotification<IrcMessage>>
{
	private readonly IRelayState _relayState;
	private readonly string _streamerNick;
	private readonly ILogger<MessageRelayService> _logger;
	private readonly ITwitchUser _twitch;
	private readonly ISteamBot _steam;
	private readonly IOsuBot _osu;

	public MessageRelayService(
		ILogger<MessageRelayService> logger,
		IRelayState relayState,
		ITwitchUser twitch,
		ISteamBot steam,
		IOsuBot osu,
		ITwitchUserConfig config)
	{
		_relayState = relayState;
		_logger = logger;
		_twitch = twitch;
		_steam = steam;
		_osu = osu;
		_streamerNick = config.TwitchChannel![1..];
	}

	public async Task Handle(RelayNotification<IrcMessage> notification, CancellationToken cancellationToken)
	{
		if (notification.Message.Content.IsCommand())
			return;

		//Only relay messages that aren't from the streamer
		if (MessageSenderIsBroadcaster(notification.Message))
			return;

		await TryRelay(notification);
	}

	private bool MessageSenderIsBroadcaster(IrcMessage message)
		=> _streamerNick.Equals(message.Sender?.Name, StringComparison.OrdinalIgnoreCase);

	private async Task TryRelay(RelayNotification<IrcMessage> notification)
	{
		var timeElapsedSinceLastMessage = DateTime.UtcNow - _relayState.LastMessage;
		var minimumWait = TimeSpan.FromMilliseconds(500);
		if (timeElapsedSinceLastMessage <= minimumWait)
		{
			var timeToWait = minimumWait - timeElapsedSinceLastMessage;

			_logger.LogInformation("Waiting {nextRetryMs}ms before relaying message.", timeToWait.TotalMilliseconds);
			await Task.Delay(timeToWait);
		}

		await Relay(notification);
		_relayState.LastMessage = DateTime.UtcNow;
	}

	private async Task Relay(RelayNotification<IrcMessage> notification)
	{
		var message = notification.Message;

		if (notification.Bot is ISteamBot)
		{
			await RelayMessage(notification, message.Content, _twitch);
			return;
		}

		// The relay request comes from TwitchBot
		var responseContent = $"{message.Sender?.CapitalizeUsername()}: {message.Content}";
		await RelayMessage(notification, responseContent, _steam);
		await RelayMessage(notification, responseContent, _osu);
	}

	private async Task RelayMessage(RelayNotification<IrcMessage> notification, string responseContent, IMessageBot<IrcMessage> relaySubject)
	{
		var response = notification.Message with { Content = responseContent };
		_logger.LogInformation("{sourceBotName} -> {relaySubjectName}: {messageContent}", notification.Bot.Name, relaySubject.Name, responseContent);

		await relaySubject.SendMessageAsync(response);
	}
}
