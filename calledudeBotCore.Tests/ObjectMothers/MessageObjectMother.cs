using calledudeBot.Chat;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class MessageObjectMother
{
    public static IrcMessage Empty { get; } = new IrcMessage(string.Empty, string.Empty, UserObjectMother.Empty);
    public static IrcMessage EmptyMod { get; set; } = new IrcMessage(string.Empty, string.Empty, UserObjectMother.EmptyMod);

    public static IrcMessage CreateWithContent(string content)
        => new(content, string.Empty, new User(string.Empty));

    public static DiscordMessage CreateDiscordMessageWithContent(string content)
        => new(content, string.Empty, UserObjectMother.Empty, 0);

    public static IrcMessage CreateEmptyWithUser(string userName)
        => new(string.Empty, string.Empty, UserObjectMother.Create(userName));
}
