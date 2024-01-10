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

namespace calledudeBot.Bots;

public interface ISteamBot : IMessageBot<IrcMessage>;

public class SteamBot : Bot<IrcMessage>, ISteamBot
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
	private CancellationToken _hostCancellationToken;
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

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_hostCancellationToken = cancellationToken;
		_cts = new CancellationTokenSource();

		using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_hostCancellationToken, _cts.Token);
		_steamClient.Connect();
		await Task.Factory.StartNew(() =>
		{
			while (!linkedTokenSource.IsCancellationRequested)
			{
				_manager.RunWaitCallbacks(TimeSpan.FromSeconds(2));
			}
		}, linkedTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	}

	private async void OnChatMessage(SteamFriends.FriendMsgCallback callback)
	{
		if (callback.EntryType != EChatEntryType.ChatMsg)
			return;

		if (callback.Message is null)
			return;

		_logger.LogInformation("Relaying message to Twitch: '{message}'", callback.Message);

		var message = new IrcMessage
		{
			Content = callback.Message,
			Sender = new User(_steamFriends.GetFriendPersonaName(callback.Sender)!, false)
		};

		await _messageDispatcher.PublishAsync(new RelayNotification<IrcMessage>(this, message));
	}

	private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
	{
		if (callback.Result != EResult.OK)
		{
			_logger.LogError("Unable to logon to Steam: {loginResult} / {loginResultExtended}", callback.Result, callback.ExtendedResult);
			return;
		}

		_logger.LogInformation("Connected to Steam.");
	}

	private void OnConnected(SteamClient.ConnectedCallback callback)
		=> _steamUser.LogOn(new SteamUser.LogOnDetails
		{
			Username = _username,
			Password = _password,
		});

	private async void OnDisconnected(SteamClient.DisconnectedCallback callback)
	{
		_cts!.Cancel();
		_logger.LogWarning("Disconnected. Re-establishing connection..");
		_cts.Dispose();
		await StartAsync(_hostCancellationToken);
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
}