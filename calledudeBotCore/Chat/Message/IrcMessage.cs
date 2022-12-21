using System;

namespace calledudeBot.Chat;

public sealed record IrcMessage : Message
{
	public static string ParseMessage(string[] buffer)
		=> string.Join(" ", buffer[3..])[1..];

	//:calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test
	public static string ParseUser(ReadOnlySpan<char> buffer)
		=> buffer[1..buffer.IndexOf('!')].ToString();
}
