using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using System;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class CommandContainerObjectMother
{
	public static Lazy<ICommandContainer> CreateLazy(params Command[] commands)
		=> new(Create(commands));

	public static CommandContainer Create(params Command[] commands)
		=> new(commands);

	public static (T, Lazy<ICommandContainer>) CreateWithSpecialCommand<T>(Func<Lazy<ICommandContainer>, T> commandFactory) where T : SpecialCommand<CommandParameter>
	{
		var lazyCommandContainer = CreateLazy();
		var command = commandFactory(lazyCommandContainer);
		lazyCommandContainer.Value.Commands.Add(command);
		return (command, lazyCommandContainer);
	}
}