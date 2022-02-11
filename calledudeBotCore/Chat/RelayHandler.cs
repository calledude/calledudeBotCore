using calledudeBot.Bots;
using calledudeBot.Chat.Commands;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Chat;

public sealed class RelayHandler : INotificationHandler<RelayNotification<IrcMessage>>
{
    private readonly RelayState _relayState;
    private readonly string _streamerNick;
    private readonly ILogger<RelayHandler> _logger;
    private readonly ITwitchUser _twitch;
    private readonly SteamBot _steam;
    private readonly OsuBot _osu;

    public RelayHandler(
        ILogger<RelayHandler> logger,
        RelayState relayState,
        ITwitchUser twitch,
        SteamBot steam,
        OsuBot osu,
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
        if (CommandUtils.IsCommand(notification.Message.Content))
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
        var timeElapsedSinceLastMessage = DateTime.Now - _relayState.LastMessage;
        var minimumWait = TimeSpan.FromMilliseconds(500);
        if (timeElapsedSinceLastMessage <= minimumWait)
        {
            var timeToWait = minimumWait - timeElapsedSinceLastMessage;

            _logger.LogInformation("Waiting {nextRetryMs}ms before relaying message.", timeToWait.TotalMilliseconds);
            await Task.Delay(timeToWait);
        }

        await Relay(notification);
        _relayState.LastMessage = DateTime.Now;
    }

    private async Task Relay(RelayNotification<IrcMessage> notification)
    {
        var message = notification.Message;

        if (notification.Bot is SteamBot)
        {
            await RelayMessage(notification, message.Content, _twitch);
        }
        else if (notification.Bot is TwitchBotBase)
        {
            var responseContent = $"{message.Sender!.Name}: {message.Content}";
            await RelayMessage(notification, responseContent, _steam);
            await RelayMessage(notification, responseContent, _osu);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private async Task RelayMessage(RelayNotification<IrcMessage> notification, string responseContent, IMessageBot<IrcMessage> relaySubject)
    {
        var response = notification.Message.CloneWithMessage(responseContent);
        _logger.LogInformation("{sourceBotName} -> {relaySubjectName}: {messageContent}", notification.Bot.Name, relaySubject.Name, responseContent);

        await relaySubject.SendMessageAsync(response);
    }
}
