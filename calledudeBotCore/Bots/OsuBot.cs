using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Config;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public interface IOsuBot : IMessageBot<IrcMessage>
{
}

public sealed class OsuBot : IOsuBot
{
	private readonly IIrcClient _ircClient;

	public string Name => "osu!";

	public OsuBot(IIrcClient ircClient, IOptions<BotConfig> options)
	{
		_ircClient = ircClient;

		var config = options.Value;
		ircClient.Server = "irc.ppy.sh";
		ircClient.SuccessCode = 376;
		ircClient.Nick = config.OsuUsername!;
		ircClient.ChannelName = config.OsuUsername;
		ircClient.Token = config.OsuIRCToken!;
		ircClient.MessageFilters = new()
		{
			"QUIT"
		};

		ircClient.Failures = new HashSet<string>
		{
			$":cho.ppy.sh 464 {ircClient.Nick} :Bad authentication token.",
		};
	}

	public async Task SendMessageAsync(IrcMessage message)
		=> await _ircClient.SendMessage(message);

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_ = Task.Run(async () =>
		{
			await _ircClient.Setup();
			await _ircClient.Start(cancellationToken);
		}, cancellationToken);

		return Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await _ircClient.Logout();
		_ircClient.Dispose();
	}
}
