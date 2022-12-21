using calledudeBot.Chat.Commands;
using calledudeBotCore.Tests.ObjectMothers;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class HelpCommandTests
{
	[Fact]
	public async Task SpecificCommand_DoesNotExist_ErrorResponse()
	{
		var (helpCommand, _) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));
		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent("!doesNotExist");

		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal("You ok there bud? Try again.", response);
	}

	[Fact]
	public async Task SpecificCommand_Elevated_Exists_NonModerator_ErrorResponse()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		var commandMock = new Mock<Command>();
		const string commandName = "!someCommand";
		commandMock.SetupGet(x => x.Name).Returns(commandName);
		commandMock.SetupGet(x => x.RequiresMod).Returns(true);

		commandContainer.Value.Commands.Add(commandMock.Object);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal("You ok there bud? Try again.", response);
	}

	[Fact]
	public async Task SpecificCommand_Elevated_Exists_IsModerator()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		var commandMock = new Mock<Command>();
		const string commandName = "!someCommand";
		commandMock.SetupGet(x => x.Name).Returns(commandName);
		commandMock.SetupGet(x => x.RequiresMod).Returns(true);

		commandContainer.Value.Commands.Add(commandMock.Object);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageAndUser(commandName, UserObjectMother.EmptyMod);
		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal($"Command '{commandName}' has no description.", response);
	}

	[Fact]
	public async Task SpecificCommand_NoElevationRequired_Exists_NonModerator()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		var commandMock = new Mock<Command>();
		const string commandName = "!someCommand";
		commandMock.SetupGet(x => x.Name).Returns(commandName);
		commandMock.SetupGet(x => x.RequiresMod).Returns(false);

		commandContainer.Value.Commands.Add(commandMock.Object);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal($"Command '{commandName}' has no description.", response);
	}

	[Fact]
	public async Task SpecificCommand_With_AlternateNames()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		const string commandName = "!someCommand";

		var command = new Command()
		{
			Name = commandName,
			AlternateName = new List<string>
			{
				"!test",
				"!something"
			}
		};

		commandContainer.Value.Commands.Add(command);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		var alts = string.Join("/", command.AlternateName);
		Assert.Equal($"Command '{commandName}/{alts}' has no description.", response);
	}

	[Fact]
	public async Task SpecificCommand_With_DescriptionAndAlternateNames()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		const string commandName = "!someCommand";
		const string description = "some nice description";
		var command = new Command()
		{
			Name = commandName,
			AlternateName = new List<string>
			{
				"!test",
				"!something"
			},
			Description = description
		};

		commandContainer.Value.Commands.Add(command);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		var alts = string.Join("/", command.AlternateName);
		Assert.Equal($"Command '{commandName}/{alts}' has the description '{description}'", response);
	}

	[Fact]
	public async Task SpecificCommand_With_Description()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		const string commandName = "!someCommand";
		const string description = "some nice description";
		var command = new Command()
		{
			Name = commandName,
			Description = description
		};

		commandContainer.Value.Commands.Add(command);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal($"Command '{commandName}' has the description '{description}'", response);
	}

	[Fact]
	public async Task SpecificCommand_Uses_NonPrefixed_Word()
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		const string commandName = "someCommand";

		var command = new Command()
		{
			Name = "!" + commandName,
		};

		commandContainer.Value.Commands.Add(command);

		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(commandName);
		var response = await helpCommand.Handle(commandParameter);

		Assert.Equal($"Command '!{commandName}' has no description.", response);
	}

	[Fact]
	public async Task NonSpecificCommand()
	{
		var (helpCommand, _) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));
		var response = await helpCommand.Handle(CommandParameterObjectMother.EmptyWithPrefixedWord);

		Assert.Equal("These are the commands you can use: !help", response);
	}

	[Theory]
	[InlineData(true, "!help", "!whatever", "!hello")]
	[InlineData(false, "!help", "!hello")]
	public async Task NonSpecificCommand_DoesNotShowElevatedCommands(bool isMod, params string[] expectedCommands)
	{
		var (helpCommand, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));

		var commandMock = new Mock<Command>();
		commandMock.SetupGet(x => x.RequiresMod).Returns(true);
		commandMock.SetupGet(x => x.Name).Returns("!whatever");

		const string secondCommandName = "!hello";
		var secondCommand = new Command()
		{
			Name = secondCommandName
		};

		commandContainer.Value.Commands.Add(commandMock.Object);
		commandContainer.Value.Commands.Add(secondCommand);

		var response = await helpCommand.Handle(CommandParameterObjectMother.CreateWithEmptyMessageAndUser(UserObjectMother.Create(string.Empty, isMod)));

		var commands = string.Join(" » ", expectedCommands);
		Assert.Equal($"These are the commands you can use: {commands}", response);
	}
}
