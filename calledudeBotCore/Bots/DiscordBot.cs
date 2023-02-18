using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public sealed class DiscordBot : Bot<DiscordMessage>
{
	private readonly IDiscordSocketClient _discordClient;
	private readonly IMessageDispatcher _dispatcher;
	private readonly ILogger<DiscordBot> _logger;
	private readonly string _token;

	public override string Name => "Discord";

	public DiscordBot(
		ILogger<DiscordBot> logger,
		IOptions<BotConfig> options,
		IDiscordSocketClient discordClient,
		IMessageDispatcher dispatcher) : base(logger)
	{
		_token = options.Value.DiscordToken!;

		_logger = logger;
		_discordClient = discordClient;
		_dispatcher = dispatcher;
	}

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_discordClient.Log += Log;
		_discordClient.MessageReceived += OnMessageReceived;
		_discordClient.Ready += () => OnReady(cancellationToken);

		await _discordClient.Login(_token);
		await _discordClient.Start();
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

	private async Task OnReady(CancellationToken cancellationToken)
		=> await _dispatcher.PublishAsync(new ReadyNotification(this), cancellationToken);

	private async Task OnMessageReceived(Discord.IMessage messageParam)
	{
		// Don't process the command if it was a System Message or if we sent it ourselves
		if (messageParam is not IUserMessage message)
			return;

		if (_discordClient.CurrentUser.Id == message.Author.Id)
			return;

		if (message.Author is not IGuildUser user)
			return;

		var msg = new DiscordMessage
		{
			Content = message.Content,
			Channel = $"#{message.Channel.Name}",
			Sender = new User($"{user.Username}#{user.Discriminator}", IsMod(user)),
			Destination = message.Channel.Id
		};

		await _dispatcher.PublishAsync(msg);
	}

	private static bool IsMod(IGuildUser user)
		=> user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers;

	protected override async Task SendMessage(DiscordMessage message)
	{
		var channel = _discordClient.GetMessageChannel(message.Destination);
		await channel!.SendMessageAsync(message.Content);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await _discordClient.Logout();
		await _discordClient.Stop();
	}
}
