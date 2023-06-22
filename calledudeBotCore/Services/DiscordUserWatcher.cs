using calledudeBot.Bots;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Utilities;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IDiscordUserWatcher
{
	Task<bool> TryWaitForUserStreamToStart();
}

public class DiscordUserWatcher : IDiscordUserWatcher
{
	private readonly ILogger<DiscordUserWatcher> _logger;
	private readonly IDiscordSocketClient _discordClient;
	private readonly IAsyncTimer _timer;
	private readonly IStreamingState _streamingState;
	private readonly ulong _announceChannelID;
	private readonly ulong _streamerID;
	private readonly AsyncAutoResetEvent _streamStarted;

	public DiscordUserWatcher(
		ILogger<DiscordUserWatcher> logger,
		IOptions<BotConfig> options,
		IDiscordSocketClient discordClient,
		IAsyncTimer timer,
		IStreamingState streamingState)
	{
		_logger = logger;
		_discordClient = discordClient;
		_streamingState = streamingState;

		_timer = timer;
		_timer.Interval = 2000;

		var config = options.Value;
		_announceChannelID = config.AnnounceChannelId;
		_streamerID = config.StreamerId;

		_streamStarted = new AsyncAutoResetEvent(false);
	}

	public async Task<bool> TryWaitForUserStreamToStart()
	{
		var announceChannel = _discordClient.GetMessageChannel(_announceChannelID) as ITextChannel;
		if (announceChannel is null)
		{
			_logger.LogWarning("Invalid channel. Will not announce when stream goes live.");
			return false;
		}

		var streamer = await announceChannel.Guild.GetUserAsync(_streamerID);
		if (streamer is null)
		{
			_logger.LogWarning("Invalid StreamerID. Could not find user.");
			return false;
		}

		_logger.LogInformation("Checking discord status for user {discordUserName}#{discriminator}..", streamer.Username, streamer.Discriminator);

		_timer.Start(async (_) => await WatchUser(streamer, announceChannel), CancellationToken.None);

		await _streamStarted.WaitAsync();
		await _timer.Stop();
		return true;
	}

	private async Task WatchUser(IGuildUser streamer, ITextChannel announceChannel)
	{
		if (streamer?.Activities.FirstOrDefault(x => x is StreamingGame) is not StreamingGame activity)
			return;

		_streamingState.IsStreaming = true;

		var messages = await announceChannel
			.GetMessagesAsync()
			.FlattenAsync();

		if (!CurrentStreamHasBeenAnnounced(messages, activity, out var announcementMessage))
		{
			_logger.LogInformation("Streamer went live. Sending announcement to #{channelName}", announceChannel.Name);
			await announceChannel.SendMessageAsync(announcementMessage);
			_streamingState.SessionId = Guid.NewGuid();
		}

		_streamStarted.Set();
	}

	//StreamStarted returns the _true_ time that the stream started
	//If any announcement message exists within 3 minutes of that, don't send a new announcement
	//In that case we assume that the bot has been restarted (for whatever reason)
	private bool CurrentStreamHasBeenAnnounced(IEnumerable<IMessage> messages, StreamingGame activity, out string announcementMessage)
	{
		var twitchUsername = activity.Url.Split('/').Last();
		var msg = $"🔴 **{twitchUsername}** is now **LIVE**\n- Title: **{activity.Details}**\n- Watch at: {activity.Url}";
		announcementMessage = msg;
		return messages.Any(m =>
						m.Author.Id == _discordClient.CurrentUser.Id
						&& m.Content.Equals(msg)
						&& _streamingState.StreamStarted - m.Timestamp < TimeSpan.FromMinutes(3));
	}
}
