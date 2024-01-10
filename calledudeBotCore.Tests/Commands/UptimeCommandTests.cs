using calledudeBot.Chat.Commands;
using calledudeBot.Models;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class UptimeCommandTests
{
	private const string _streamUptime = "Stream uptime:";
	private readonly IStreamingState _streamingState;
	public UptimeCommandTests()
	{
		_streamingState = Substitute.For<IStreamingState>();
	}

	[Fact]
	public async Task NotStreaming_Returns_StreamerIsntLive()
	{
		_streamingState.IsStreaming.Returns(false);

		var target = new UptimeCommand(_streamingState);
		var result = await target.Handle();

		Assert.Equal("Streamer isn't live.", result);
	}

	[Theory]
	[InlineData(1, 0, 0, $"{_streamUptime} 1h")]
	[InlineData(0, 5, 0, $"{_streamUptime} 5m")]
	[InlineData(0, 0, 10, $"{_streamUptime} 10s")]
	[InlineData(2, 0, 1, $"{_streamUptime} 2h 1s")]
	[InlineData(2, 3, 15, $"{_streamUptime} 2h 3m 15s")]
	[InlineData(2, 3, 0, $"{_streamUptime} 2h 3m")]
	[InlineData(0, 0, 0, "The stream has just started.")] //Why?
	public async Task Streaming(int hoursStreamed, int minutesStreamed, int secondsStreamed, string expectedOutput)
	{
		_streamingState.IsStreaming.Returns(true);
		var streamStarted = DateTime.UtcNow.AddHours(-hoursStreamed).AddMinutes(-minutesStreamed).AddSeconds(-secondsStreamed);
		_streamingState.StreamStarted.Returns(streamStarted);

		var target = new UptimeCommand(_streamingState);
		var result = await target.Handle();

		Assert.Equal(expectedOutput, result);
	}
}
