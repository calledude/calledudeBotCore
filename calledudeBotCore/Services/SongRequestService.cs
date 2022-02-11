using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public sealed class SongRequestService : INotificationHandler<IrcMessage>
{
    private const string SONGREQUESTLINK = "https://osu.ppy.sh/api/get_beatmaps?k={0}&b={1}";

    private static readonly Regex _beatmapRegex = new(@"https?://osu.ppy.sh/(?:b|beatmapsets/.+?)/(?<BeatmapID>\d+)", RegexOptions.Compiled);

    private readonly string _osuAPIToken;
    private readonly OsuBot _osuBot;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<SongRequestService> _logger;
    private readonly IMessageBot<IrcMessage> _twitchBot;

    public SongRequestService(
        IOptions<BotConfig> options,
        IMessageBot<IrcMessage> twitchBot,
        OsuBot osuBot,
        IHttpClientFactory clientFactory,
        ILogger<SongRequestService> logger)
    {
        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _osuAPIToken = config.OsuAPIToken!;
        _osuBot = osuBot;
        _clientFactory = clientFactory;
        _logger = logger;
        _twitchBot = twitchBot;
    }

    //[http://osu.ppy.sh/b/795232 fhana - Wonder Stella [Stella]]
    public async Task Handle(IrcMessage notification, CancellationToken cancellationToken)
    {
        var match = _beatmapRegex.Match(notification.Content);
        if (!match.Success)
            return;

        var beatmapID = match.Groups["BeatmapID"];
        var reqLink = string.Format(SONGREQUESTLINK, _osuAPIToken, beatmapID);

        var client = _clientFactory.CreateClient();
        var (success, osuSongs) = await client.GetAsJsonAsync<OsuSong[]>(reqLink);

        if (!success)
        {
            _logger.LogError("Requesting osu! song data was unsuccessful.");
            return;
        }

        if (osuSongs != default)
        {
            var song = osuSongs.Single();
            var response = notification.CloneWithMessage($"{notification.Sender!.Name} requested song: [https://osu.ppy.sh/b/{beatmapID} {song.Artist} - {song.Title} [{song.BeatmapVersion}]]");
            await _osuBot.SendMessageAsync(response);
        }
        else
        {
            var response = notification.CloneWithMessage("I couldn't find that song, sorry.");
            await _twitchBot.SendMessageAsync(response);
        }
    }
}
