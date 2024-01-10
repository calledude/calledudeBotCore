using calledudeBot.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public sealed class UptimeCommand : SpecialCommand
{
	private readonly IStreamingState _streamingState;

	public UptimeCommand(IStreamingState streamingState)
	{
		Name = "!uptime";
		Description = "Shows how long the stream has been live";
		AlternateName = ["!live"];
		RequiresMod = false;
		_streamingState = streamingState;
	}

	public override Task<string> Handle()
	{
		var wentLiveAt = WentLiveAt();
		if (wentLiveAt == default)
			return Task.FromResult("Streamer isn't live.");

		var timeSinceLive = DateTime.UtcNow - wentLiveAt;

		if (timeSinceLive.TotalSeconds < 5)
			return Task.FromResult("The stream has just started.");

		var sb = new StringBuilder();

		sb.Append("Stream uptime: ");

		if (timeSinceLive.Hours > 0)
			sb.Append(timeSinceLive.Hours).Append("h ");

		if (timeSinceLive.Minutes > 0)
			sb.Append(timeSinceLive.Minutes).Append("m ");

		if (timeSinceLive.Seconds > 0)
			sb.Append(timeSinceLive.Seconds).Append('s');

		return Task.FromResult(sb.ToString().TrimEnd());
	}

	private DateTime WentLiveAt()
		=> _streamingState.IsStreaming
			? _streamingState.StreamStarted
			: default;
}