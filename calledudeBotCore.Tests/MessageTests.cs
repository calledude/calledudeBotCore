using calledudeBot.Chat;
using Xunit;

namespace calledudeBotCore.Tests;

public class MessageTests
{
	[Fact]
	public void ParseMessage_ParsesCorrectly()
	{
		const string message = ":calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test";

		var parsedMessage = IrcMessage.ParseMessage(message.Split());

		Assert.Equal("test", parsedMessage);
	}

	[Fact]
	public void ParseUser_ParsesUserCorrectly()
	{
		const string message = ":calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test";

		var parsedUser = IrcMessage.ParseUser(message);

		Assert.Equal("calledude", parsedUser);
	}
}
