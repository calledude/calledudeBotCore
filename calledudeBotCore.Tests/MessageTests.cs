using calledudeBot.Chat;
using calledudeBotCore.Tests.ObjectMothers;
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

        Assert.Equal("Calledude", parsedUser);
    }

    [Fact]
    public void IRC_CloneMessage_WithMessage_UsesNewMessage()
    {
        var ircMessage = new IrcMessage("test", "#calledude", UserObjectMother.Empty);

        const string expectedContent = "hello";
        var clonedMessage = ircMessage.CloneWithMessage(expectedContent);

        Assert.Equal(expectedContent, clonedMessage.Content);
        Assert.Equal(ircMessage.Channel, clonedMessage.Channel);
        Assert.Equal(ircMessage.Sender, clonedMessage.Sender);
    }

    [Fact]
    public void DISCORD_CloneMessage_WithMessage_UsesNewMessage()
    {
        var discordMessage = new DiscordMessage("test", "#general", UserObjectMother.EmptyMod, 1234);

        const string expectedContent = "hello";
        var clonedMessage = discordMessage.CloneWithMessage(expectedContent);

        Assert.Equal(expectedContent, clonedMessage.Content);
        Assert.Equal(discordMessage.Channel, clonedMessage.Channel);
        Assert.Equal(discordMessage.Sender, clonedMessage.Sender);
        Assert.Equal(discordMessage.Destination, clonedMessage.Destination);
    }
}
