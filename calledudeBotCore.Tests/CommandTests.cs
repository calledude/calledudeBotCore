using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBotCore.Tests.ObjectMothers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests;

public class CommandTests
{
	[Fact]
	public async Task Custom_Command_Only_Alternate_Is_Deleted()
	{
		var (deleteCmd, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new DeleteCommand(container));

		const string alternateName = "!yo";
		const string cmdName = "!hi";
		var commandToDelete = new Command()
		{
			Name = cmdName,
			Response = "hi :)",
			AlternateName = new List<string> { alternateName }
		};

		commandContainer.Value.Commands.Add(commandToDelete);

		var messageParams = $"{deleteCmd.Name} {alternateName}".Split();
		var response = await deleteCmd.Handle(new CommandParameter<IrcMessage>(messageParams, new IrcMessage("", null, null)));

		Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Key == alternateName);
		Assert.Contains(commandContainer.Value.Commands, x => x.Key == cmdName);
		Assert.Equal($"Deleted alternative command '{alternateName}'", response);
	}

	[Fact]
	public async Task Custom_Command_And_Alternates_Gets_Deleted_Properly()
	{
		var (deleteCmd, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new DeleteCommand(container));

		const string alternateName = "!yo";
		const string cmdName = "!hi";
		var commandToDelete = new Command()
		{
			Name = cmdName,
			Response = "hi :)",
			AlternateName = new List<string> { alternateName }
		};

		commandContainer.Value.Commands.Add(commandToDelete);
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{deleteCmd.Name} {commandToDelete.Name}");

		var response = await deleteCmd.Handle(commandParameter);

		Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Key == cmdName || x.Key == alternateName);
		Assert.Equal($"Deleted command '{commandToDelete.Name}'", response);
	}
}
