using calledudeBot.Chat;
using Xunit;

namespace calledudeBotCore.Tests
{
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

            Assert.Equal("Calledude", parsedUser);
        }

        [Fact]
        public void IRC_CloneMessage_WithMessage_UsesNewMessage()
        {
            const string user = "calledude";
            const string message = "test";

            var ircMessage = new IrcMessage(message, "#calledude", new User(user, true));

            const string expectedContent = "hello";
            var clonedMessage = ircMessage.CloneWithMessage(expectedContent);

            Assert.Equal(expectedContent, clonedMessage.Content);
            Assert.Equal(ircMessage.Channel, clonedMessage.Channel);
            Assert.Equal(ircMessage.Sender, clonedMessage.Sender);
        }

        [Fact]
        public void DISCORD_CloneMessage_WithMessage_UsesNewMessage()
        {
            var discordMessage = new DiscordMessage("test", "#general", new User("calledude#1914", true), 1234);
            var clonedMessage = discordMessage.CloneWithMessage("hello");

            Assert.Equal("hello", clonedMessage.Content);
            Assert.Equal(discordMessage.Channel, clonedMessage.Channel);
            Assert.Equal(discordMessage.Sender, clonedMessage.Sender);
            Assert.Equal(discordMessage.Destination, clonedMessage.Destination);
        }
    }
}
