using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IOsuUserService : INotificationHandler<ReadyNotification>
{
	Task<OsuUser?> GetOsuUser(string username);
}

public sealed class OsuUserService : IOsuUserService
{
	private OsuUser? _oldOsuData;
	private readonly IAsyncTimer _checkTimer;
	private readonly IHttpClientWrapper _client;
	private readonly ITwitchBot _twitch;
	private readonly ILogger<OsuUserService> _logger;
	private readonly string _osuAPIToken;
	private readonly string _osuNick;

	public OsuUserService(
		IHttpClientWrapper client,
		IOptions<BotConfig> options,
		ITwitchBot twitchBot,
		ILogger<OsuUserService> logger,
		IAsyncTimer timer)
	{
		var config = options.Value;
		_osuAPIToken = config.OsuAPIToken!;
		_osuNick = config.OsuUsername!;

		_client = client;
		_twitch = twitchBot;
		_logger = logger;
		_checkTimer = timer;
	}

	public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
	{
		if (notification.Bot is not ITwitchBot)
			return Task.CompletedTask;

		_checkTimer.Interval = 15000;
		_checkTimer.Start(CheckUserUpdate, cancellationToken);
		return Task.CompletedTask;
	}

	private async Task<OsuUser?> GetOsuUserInternal(string? username = default)
	{
		var user = username ?? _osuNick;

		var requestUrl = string.Format("https://osu.ppy.sh/api/get_user?k={0}&u={1}", _osuAPIToken, user);

		var (success, users) = await _client.GetAsJsonAsync<OsuUser[]>(requestUrl, SerializerContext.CaseInsensitive.OsuUserArray);
		if (!success)
			return null;

		return users?.Single() ?? throw new Exception("Response from osu! was null (possibly invalid username)");
	}

	public async Task<OsuUser?> GetOsuUser(string username)
		=> await GetOsuUserInternal(username);

	private async Task CheckUserUpdate(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		var newUserState = await GetOsuUserInternal();
		if (newUserState == default)
		{
			_logger.LogError("Requesting osu! user data was unsuccessful.");
			return;
		}

		await CheckUserUpdate(newUserState);
	}

	private async Task CheckUserUpdate(OsuUser user)
	{
		if (_oldOsuData == null || _oldOsuData == user)
		{
			_oldOsuData = user;
			return;
		}

		if (Math.Abs(user.PP - _oldOsuData.PP) < 0.1)
			return;

		var rankDiff = user.Rank - _oldOsuData.Rank;
		var ppDiff = user.PP - _oldOsuData.PP;

		var formatted = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Math.Abs(ppDiff));
		var totalPP = user.PP.ToString(CultureInfo.InvariantCulture);

		var rankMessage = $"{Math.Abs(rankDiff)} ranks (#{user.Rank}).";
		var ppMessage = $"PP: {(ppDiff < 0 ? "-" : "+")}{formatted}pp ({totalPP}pp)";

		var newRankMessage = new IrcMessage
		{
			Content = $"{user.Username} just {(rankDiff < 0 ? "gained" : "lost")} {rankMessage} {ppMessage}"
		};

		await _twitch.SendMessageAsync(newRankMessage);
	}
}
