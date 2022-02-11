using calledudeBot.Chat;
using calledudeBot.Chat.Info;
using System.Collections.Generic;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class CommandParameterObjectMother
{
    public static CommandParameter EmptyWithPrefixedWord { get; } = new CommandParameter<IrcMessage>(new List<string> { "!" }, MessageObjectMother.Empty);

    public static CommandParameter CreateWithMessageContent(string content)
        => new CommandParameter<IrcMessage>(new List<string> { "!", content }, MessageObjectMother.Empty);
}
