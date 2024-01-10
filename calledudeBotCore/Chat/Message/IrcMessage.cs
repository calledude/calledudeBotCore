using System;

namespace calledudeBot.Chat;

public sealed record IrcMessage : Message
{
	public static (string user, string message) ParseMessage(ReadOnlySpan<char> buffer, Span<Range> ranges, int splitCount)
	{
		var user = ParseUser(buffer[ranges[0]]);

		if (splitCount <= 3)
			return (user, string.Empty);

		var message = buffer[ranges[3]][1..].ToString();
		return (user, message);
	}

	//:calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test
	public static string ParseUser(ReadOnlySpan<char> buffer)
		=> buffer[1..buffer.IndexOf('!')].ToString();
}