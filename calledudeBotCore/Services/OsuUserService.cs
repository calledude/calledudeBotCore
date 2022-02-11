using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IOsuUserService : INotificationHandler<ReadyNotification>, IDisposable
{
    Task<OsuUser?> GetOsuUser(string username);
}

public sealed class OsuUserService : IOsuUserService
{
    private OsuUser? _oldOsuData;
    private readonly Timer _checkTimer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMessageBot<IrcMessage> _twitch;
    private readonly ILogger<OsuUserService> _logger;
    private readonly string _osuAPIToken;
    private readonly string _osuNick;

    public OsuUserService(
        IHttpClientFactory httpClientFactory,
        IOptions<BotConfig> options,
        IMessageBot<IrcMessage> twitchBot,
        ILogger<OsuUserService> logger)
    {
        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _osuAPIToken = config.OsuAPIToken ?? throw new ArgumentNullException("config.OsuAPIToken");
        _osuNick = config.OsuUsername ?? throw new ArgumentNullException("config.OsuUsername");

        _httpClientFactory = httpClientFactory;
        _twitch = twitchBot;
        _logger = logger;
        _checkTimer = new Timer(CheckUserUpdate, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Bot is not TwitchBot)
            return Task.CompletedTask;

        _checkTimer.Change(0, 15000);
        return Task.CompletedTask;
    }

    private async Task<OsuUser?> GetOsuUserInternal(string? username = default)
    {
        var user = username ?? _osuNick;

        var requestUrl = string.Format("https://osu.ppy.sh/api/get_user?k={0}&u={1}", _osuAPIToken, user);

        var client = _httpClientFactory.CreateClient();
        var (success, users) = await client.GetAsJsonAsync<OsuUser[]>(requestUrl);

        if (!success)
            return null;

        return users?.Single() ?? throw new Exception("Response from osu! was null (possibly invalid username)");
    }

    public async Task<OsuUser?> GetOsuUser(string username)
        => await GetOsuUserInternal(username);

    private async void CheckUserUpdate(object? state)
    {
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
        if (_oldOsuData != null
            && _oldOsuData.Rank != user.Rank
            && Math.Abs(user.PP - _oldOsuData.PP) >= 0.1)
        {
            var rankDiff = user.Rank - _oldOsuData.Rank;
            var ppDiff = user.PP - _oldOsuData.PP;

            var formatted = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Math.Abs(ppDiff));
            var totalPP = user.PP.ToString(CultureInfo.InvariantCulture);

            var rankMessage = $"{Math.Abs(rankDiff)} ranks (#{user.Rank}). ";
            var ppMessage = $"PP: {(ppDiff < 0 ? "-" : "+")}{formatted}pp ({totalPP}pp)";

            var newRankMessage = new IrcMessage($"{user.Username} just {(rankDiff < 0 ? "gained" : "lost")} {rankMessage} {ppMessage}", "", new User(""));

            await _twitch.SendMessageAsync(newRankMessage);
        }

        _oldOsuData = user;
    }

    public void Dispose()
        => _twitch.Dispose();
}
