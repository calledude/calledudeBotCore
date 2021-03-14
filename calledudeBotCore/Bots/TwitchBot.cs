using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace calledudeBot.Bots
{
    public sealed class TwitchBot : TwitchBotBase
    {
        private HashSet<string>? _mods;
        private DateTime _lastModCheck;
        private readonly IMessageDispatcher _dispatcher;
        private readonly Regex _joinRegex;
        private readonly ILogger<TwitchBot> _logger;
        private readonly Channel<string[]> _modsChan;

        public override string Name => "TwitchBot";

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

            _dispatcher = dispatcher;
            _modsChan = Channel.CreateBounded<string[]>(1);
            _joinRegex = new Regex(@":(?<User>\w+)!\k<User>@\k<User>\.tmi\.twitch\.tv (?<ParticipationType>JOIN|PART).+", RegexOptions.Compiled);
        }

        public async Task OnReady()
        {
            await IrcClient.WriteLine("CAP REQ :twitch.tv/commands");
            await IrcClient.WriteLine("CAP REQ :twitch.tv/membership");
        }

        public async Task HandleMessage(string message, string user)
        {
            var sender = new User(user, () => IsMod(user));
            var msg = new IrcMessage(message, ChannelName, sender);

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
            var moderatorMessage = $":tmi.twitch.tv NOTICE {ChannelName} :The moderators of this channel are:";
            if (buffer.StartsWith(moderatorMessage))
            {
                var modsIndex = moderatorMessage.Length;
                var modsArr = buffer[modsIndex..].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                await _modsChan.Writer.WriteAsync(modsArr);

                return;
            }

            var match = _joinRegex.Match(buffer);
            if (match.Success)
            {
                var (logUserAction, participationType) = match.Groups["ParticipationType"].Value == "JOIN"
                    ? ("joined", ParticipationType.Join)
                    : ("left", ParticipationType.Leave);

                _logger.LogInformation("User: {0} {1} the chat.", match.Groups["User"].Value, logUserAction);

                await _dispatcher.PublishAsync(
                        new UserParticipationNotification(
                            new User(match.Groups["User"].Value), ParticipationType.Join));
            }
        }

        public async Task<HashSet<string>> GetMods()
        {
            if (DateTime.Now <= _lastModCheck.AddMinutes(1))
                return _mods!;

            _logger.LogInformation("Getting moderators");
            _lastModCheck = DateTime.Now;

            await IrcClient.WriteLine($"PRIVMSG {ChannelName} /mods");

            var modsArr = await _modsChan.Reader.ReadAsync();

            _mods = modsArr.ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("Fetched moderators: {0}", _mods);

            return _mods;
        }
    }
}