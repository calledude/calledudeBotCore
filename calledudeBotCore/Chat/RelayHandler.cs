using calledudeBot.Bots;
using calledudeBot.Chat.Commands;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Chat
{
    public sealed class RelayHandler : INotificationHandler<RelayNotification<IrcMessage>>
    {
        private DateTime _lastMessage;
        private readonly string _streamerNick;
        private readonly ILogger<RelayHandler> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RelayHandler(
            ILogger<RelayHandler> logger,
            IServiceProvider serviceProvider,
            ITwitchUserConfig config)
        {
            _lastMessage = DateTime.Now;
            _logger = logger;
            _serviceProvider = serviceProvider;
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
            var timeElapsedSinceLastMessage = DateTime.Now - _lastMessage;
            var minimumWait = TimeSpan.FromMilliseconds(500);
            if (timeElapsedSinceLastMessage <= minimumWait)
            {
                var timeToWait = minimumWait - timeElapsedSinceLastMessage;

                _logger.LogInformation("Waiting {0}ms before relaying message.", timeToWait.TotalMilliseconds);
                await Task.Delay(timeToWait);
            }

            await Relay(notification);
            _lastMessage = DateTime.Now;
        }

        private async Task Relay(RelayNotification<IrcMessage> notification)
        {
            var message = notification.Message;
            string responseContent;
            IMessageBot<IrcMessage> relaySubject;

            if (notification.Bot is SteamBot)
            {
                responseContent = message.Content;
                relaySubject = _serviceProvider.GetRequiredService<ITwitchUser>();
            }
            else if (notification.Bot is TwitchBotBase)
            {
                responseContent = $"{message.Sender!.Name}: {message.Content}";
                relaySubject = _serviceProvider.GetRequiredService<SteamBot>();
            }
            else
            {
                throw new InvalidOperationException();
            }

            var response = message.CloneWithMessage(responseContent);
            _logger.LogInformation("{0} -> {1}: {2}", notification.Bot.Name, relaySubject.Name, response.Content);

            await relaySubject.SendMessageAsync(response);
        }
    }
}
