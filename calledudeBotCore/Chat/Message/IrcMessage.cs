namespace calledudeBot.Chat;

public sealed class IrcMessage : Message<IrcMessage>
{
    public IrcMessage(string message, string channel, User sender)
        : base(message, channel, sender)
    {
    }

    public static string ParseMessage(string[] buffer)
        => string.Join(" ", buffer[3..])[1..];

    //:calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test
    public static string ParseUser(string buffer)
        => buffer[1..buffer.IndexOf('!')];

    public override IrcMessage CloneWithMessage(string message)
        => new(message, Channel!, Sender!);
}
