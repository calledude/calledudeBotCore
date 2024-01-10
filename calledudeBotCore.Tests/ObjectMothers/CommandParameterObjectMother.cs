using calledudeBot.Chat;
using calledudeBot.Chat.Info;
using System.Collections.Generic;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class CommandParameterObjectMother
{
    private static readonly List<string> _prefix = ["!someCommand"];

    public static CommandParameter EmptyWithPrefixedWord => new CommandParameter<IrcMessage>(_prefix, MessageObjectMother.Empty);

    public static CommandParameter CreateWithPrefixedMessageContent(string content)
        => new CommandParameter<IrcMessage>(new List<string> { "!someCommand", content }, MessageObjectMother.Empty);

    public static CommandParameter CreateWithMessageContent(string content)
        => new CommandParameter<IrcMessage>(content.Split(), MessageObjectMother.Empty);

    public static CommandParameter CreateWithMessageContentAsMod(string content)
        => new CommandParameter<IrcMessage>(content.Split(), MessageObjectMother.EmptyMod);

    public static CommandParameter CreateWithEmptyMessageAndUserName(string userName)
        => new CommandParameter<IrcMessage>(_prefix, MessageObjectMother.CreateEmptyWithUser(userName));

    public static CommandParameter CreateWithEmptyMessageAndUser(User user)
    => new CommandParameter<IrcMessage>(_prefix, MessageObjectMother.CreateEmptyWithUser(user));

    public static CommandParameter CreateWithPrefixedMessageAndUser(string content, User user)
    => new CommandParameter<IrcMessage>(new List<string> { "!someCommand", content }, MessageObjectMother.CreateEmptyWithUser(user));
}
