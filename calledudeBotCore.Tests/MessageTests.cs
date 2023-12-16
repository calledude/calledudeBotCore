using calledudeBot.Chat;
using System;
using Xunit;

namespace calledudeBotCore.Tests;

public class MessageTests
{
	[Fact]
	public void ParseMessageAndUser()
	{
		const string message = ":calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test";
		var messageSpan = message.AsSpan();
		Span<Range> ranges = stackalloc Range[4];
		var splitCount = messageSpan.Split(ranges, ' ');

		var (parsedUser, parsedMessage) = IrcMessage.ParseMessage(messageSpan, ranges, splitCount);

		Assert.Equal("test", parsedMessage);
		Assert.Equal("calledude", parsedUser);
	}

	[Fact]
	public void ParseUser_ParsesUserCorrectly()
	{
		const string message = ":calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test";

		var parsedUser = IrcMessage.ParseUser(message);

		Assert.Equal("calledude", parsedUser);
	}
}
