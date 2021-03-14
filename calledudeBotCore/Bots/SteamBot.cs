using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamKit2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots
{
    public class SteamBot : Bot<IrcMessage>
    {
        private readonly string _username;
        private readonly string _password;
        private SteamID? _calledude;
        private readonly SteamClient _steamClient;
        private readonly CallbackManager _manager;
        private readonly SteamUser _steamUser;
        private readonly SteamFriends _steamFriends;
        private readonly ILogger<SteamBot> _logger;
        private readonly IMessageDispatcher _messageDispatcher;
        private CancellationTokenSource? _cts;

        public override string Name => "Steam";

        public SteamBot(ILogger<SteamBot> logger, IMessageDispatcher messageDispatcher, IOptions<BotConfig> options)
            : base(logger)
        {
            var config = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _username = config.SteamUsername!;
            _password = config.SteamPassword!;
            _logger = logger;
            _messageDispatcher = messageDispatcher;

            _steamClient = new SteamClient();
            _manager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>()!;
            _steamFriends = _steamClient.GetHandler<SteamFriends>()!;

            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);

            _manager.Subscribe<SteamUser.AccountInfoCallback>(_ => _steamFriends.SetPersonaState(EPersonaState.Online));
            _manager.Subscribe<SteamFriends.FriendMsgCallback>(OnChatMessage);

            _manager.Subscribe<SteamFriends.FriendsListCallback>(c => _calledude = c.FriendList[0].SteamID);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _steamClient.Connect();
            return Task.Factory.StartNew(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    _manager.RunWaitCallbacks();
                }
            }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType != EChatEntryType.ChatMsg)
                return;

            if (callback.Message == null)
                return;

            _logger.LogInformation("Relaying message to Twitch: '{0}'", callback.Message);

            var user = new User(_steamFriends.GetFriendPersonaName(callback.Sender)!, false);
            var message = new IrcMessage(callback.Message, "STEAM", user);
            await _messageDispatcher.PublishAsync(new RelayNotification<IrcMessage>(this, message));
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                _logger.LogError("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
                return;
            }

            _logger.LogInformation("Connected to {0}.", "Steam");
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = _username,
                Password = _password,
            });
        }

        private async void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            _cts!.Cancel();
            _logger.LogWarning("Disconnected. Re-establishing connection..");
            _cts.Dispose();
            await StartAsync(CancellationToken.None);
        }

        protected override Task SendMessage(IrcMessage message)
        {
            _steamFriends.SendChatMessage(_calledude!, EChatEntryType.ChatMsg, message.Content);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _steamClient.Disconnect();
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}