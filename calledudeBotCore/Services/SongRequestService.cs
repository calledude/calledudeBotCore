using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public sealed partial class SongRequestService : INotificationHandler<IrcMessage>
{
	private const string _songRequestUrl = "https://osu.ppy.sh/api/get_beatmaps?k={0}&b={1}";

	private static readonly Regex _beatmapRegex = BeatmapRegex();

	private readonly string _osuAPIToken;
	private readonly IOsuBot _osuBot;
	private readonly IHttpClientWrapper _client;
	private readonly ILogger<SongRequestService> _logger;
	private readonly ITwitchBot _twitchBot;

	public SongRequestService(
		IOptions<BotConfig> options,
		ITwitchBot twitchBot,
		IOsuBot osuBot,
		IHttpClientWrapper client,
		ILogger<SongRequestService> logger)
	{
		_osuAPIToken = options.Value.OsuAPIToken!;
		_osuBot = osuBot;
		_client = client;
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
		var reqLink = string.Format(_songRequestUrl, _osuAPIToken, beatmapID);

		var (success, osuSongs) = await _client.GetAsJsonAsync<OsuSong[]>(reqLink, SerializerContext.CaseInsensitive.OsuSongArray);

		if (!success)
		{
			_logger.LogError("Requesting osu! song data was unsuccessful.");
			return;
		}

		if (osuSongs != default)
		{
			var song = osuSongs.Single();
			var response = notification with
			{
				Content = $"{notification.Sender!.Name} requested song: [https://osu.ppy.sh/b/{beatmapID} {song.Artist} - {song.Title} [{song.BeatmapVersion}]]"
			};

			await _osuBot.SendMessageAsync(response);
		}
		else
		{
			var response = notification with { Content = "I couldn't find that song, sorry." };
			await _twitchBot.SendMessageAsync(response);
		}
	}

	[GeneratedRegex("https?://osu.ppy.sh/(?:b|beatmapsets/.+?)/(?<BeatmapID>\\d+)", RegexOptions.Compiled)]
	private static partial Regex BeatmapRegex();
}