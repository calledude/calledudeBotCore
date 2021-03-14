using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots
{
    public sealed class DiscordBot : Bot<DiscordMessage>
    {
        private readonly DiscordSocketClient _bot;
        private readonly ulong _announceChanID;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ILogger<DiscordBot> _logger;
        private readonly string _token;

        public override string Name => "Discord";

        public DiscordBot(
            ILogger<DiscordBot> logger,
            IOptions<BotConfig> options,
            DiscordSocketClient bot,
            IMessageDispatcher dispatcher) : base(logger)
        {
            var config = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _logger = logger;
            _bot = bot;
            _token = config.DiscordToken!;

            _announceChanID = config.AnnounceChannelId!;
            _dispatcher = dispatcher;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _bot.Log += Log;
            _bot.MessageReceived += OnMessageReceived;
            _bot.Ready += OnReady;

            await _bot.LoginAsync(TokenType.Bot, _token);
            await _bot.StartAsync();
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(message.Exception, message.Message ?? "An exception bubbled up: ");
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(message.ToString(prependTimestamp: false));
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(message.ToString(prependTimestamp: false));
                    break;
                case LogSeverity.Error:
                    _logger.LogError(message.Exception, message.Message ?? "An exception bubbled up: ");
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(message.ToString(prependTimestamp: false));
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(message.ToString(prependTimestamp: false));
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task OnReady()
            => await _dispatcher.PublishAsync(new ReadyNotification(this));

        private async Task OnMessageReceived(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message or if we sent it ourselves
            if (messageParam is not SocketUserMessage message)
                return;

            if (_bot.CurrentUser.Id == message.Author.Id)
                return;

            if (message.Author is not SocketGuildUser user)
                return;

            var msg = new DiscordMessage(
                message.Content,
                $"#{message.Channel.Name}",
                new User($"{user.Username}#{user.Discriminator}", () => IsMod(user)),
                message.Channel.Id);

            await _dispatcher.PublishAsync(msg);
        }

        private Task<bool> IsMod(SocketGuildUser user)
        {
            var isMod = user.GuildPermissions.BanMembers
                || user.GuildPermissions.KickMembers
                || (GetModerators()?.Any(u => u.Id == user.Id) ?? false);

            return Task.FromResult(isMod);
        }

        private IEnumerable<SocketGuildUser>? GetModerators()
        {
            var channel = _bot.GetChannel(_announceChanID) as IGuildChannel;
            var roles = channel?.Guild.Roles.Cast<SocketRole>();
            return roles?
                .Where(x => x.Permissions.BanMembers || x.Permissions.KickMembers)
                .SelectMany(r => r.Members);
        }

        protected override async Task SendMessage(DiscordMessage message)
        {
            var channel = _bot.GetChannel(message.Destination) as IMessageChannel;
            await channel!.SendMessageAsync(message.Content);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _bot.LogoutAsync();
            await _bot.StopAsync();
        }

        protected override void Dispose(bool disposing)
            => _bot.Dispose();
    }
}