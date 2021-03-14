namespace calledudeBot.Chat
{
    public sealed class IrcMessage : Message<IrcMessage>
    {
        //TODO: Remove?
        public IrcMessage(string message) : base(message)
        {
        }

        public IrcMessage(string message, string channel, User sender)
            : base(message, channel, sender)
        {
        }

        public static string ParseMessage(string[] buffer)
            => string.Join(" ", buffer[3..])[1..];

        //:calledude!calledude@calledude.tmi.twitch.tv PRIVMSG #calledude :test
        public static string ParseUser(string buffer)
        {
            var indexUpper = buffer.IndexOf('!');
            var name = buffer[1..indexUpper];

            return char.ToUpper(name[0]) + name[1..]; //capitalize first letter in username
        }

        public override IrcMessage CloneWithMessage(string message)
            => new(message, Channel!, Sender!);
    }
}