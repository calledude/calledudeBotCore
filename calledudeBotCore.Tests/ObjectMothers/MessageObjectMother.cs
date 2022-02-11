using calledudeBot.Chat;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class MessageObjectMother
{
    public static IrcMessage Empty { get; } = new IrcMessage(string.Empty, string.Empty, UserObjectMother.Empty);

    public static IrcMessage CreateWithContent(string content)
        => new IrcMessage(content, string.Empty, new User(string.Empty));
}
