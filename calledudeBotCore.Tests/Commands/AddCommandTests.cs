using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBotCore.Tests.ObjectMothers;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class AddCommandTests
{
	private readonly AddCommand _target;
	private readonly Mock<ICommandContainer> _commandContainer;
	private readonly Dictionary<string, Command> _commands = new();

	public AddCommandTests()
	{
		_commandContainer = new Mock<ICommandContainer>();
		_commandContainer.SetupGet(x => x.Commands).Returns(_commands);

		var lazyCommandContainer = new Lazy<ICommandContainer>(_commandContainer.Object);
		var (add, _) = CommandContainerObjectMother.CreateWithSpecialCommand(_ => new AddCommand(lazyCommandContainer));
		_target = add;
	}

	[Fact]
	public void AddCommand_RequiresMod() => Assert.True(_target.RequiresMod);

	[Theory]
	[InlineData("!addcmd")]
	[InlineData("!addcmd qwe qwe qwe")]
	[InlineData("!addcmd !qwe")]
	public async Task InsufficientAmountOfArguments(string messageContent)
	{
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You ok there bud? Try again.", response);

		_commandContainer.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task EditingSpecialCommandNotAllowed_SpecialCommand()
		=> await EditingSpecialCommandNotAllowed<SpecialCommand>();

	[Fact]
	public async Task EditingSpecialCommandNotAllowed_SpecialCommand_CommandParameter()
	=> await EditingSpecialCommandNotAllowed<SpecialCommand<CommandParameter>>();

	private async Task EditingSpecialCommandNotAllowed<T>() where T : Command
	{
		var specialCommand = new Mock<T>();
		specialCommand.SetupGet(x => x.Name).Returns("!something");
		_commands.Add(specialCommand.Object);

		var messageContent = $"{_target.Name} !something word";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You can't change a special command.", response);
	}

	[Fact]
	public async Task InvalidCharactersInCommandNotAllowed()
	{
		var messageContent = $"{_target.Name} !<containsInvalidCharacters> word";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("Special characters in commands are not allowed.", response);
	}

	[Fact]
	public async Task NewCommandIsAdded()
	{
		var messageContent = $"{_target.Name} !test nah fam <nice>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Contains(_commands,
			x => x.Value.Name == "!test"
				&& x.Value.Description == "nice"
				&& x.Value.Response == "nah fam");

		Assert.Equal("Added command '!test'", response);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Once);
	}

	[Fact]
	public async Task NewCommand_ConflictingCommandName()
	{
		const string conflictingName = "!test";
		const string commandName = "!somethingElse";
		_commands.Add(new Command
		{
			Name = "!somethingElse",
			AlternateName = new List<string>
			{
				conflictingName
			}
		});

		var messageContent = $"{_target.Name} {conflictingName} nah fam <nice>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Conflicting command name usage found in command '{commandName}'", response);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Never);
	}

	[Fact]
	public async Task ExistingCommand_NoChangesRequired()
	{
		const string commandName = "!test";
		const string commandResponse = "nah fam";
		const string description = "nice";
		var alternateNames = new List<string>
		{
			"!hello",
			"!ping"
		};

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = commandResponse,
			Description = description,
			AlternateName = alternateNames
		};

		_commands.Add(existingCommand);

		var alternates = string.Join(" ", alternateNames);
		var messageContent = $"{_target.Name} {commandName} {alternates} {commandResponse} <{description}>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Command '{existingCommand.Name}' already exists.", response);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Never);
	}

	[Theory]
	[InlineData("hello :)", "waddup ;D", "", "", "response")]
	[InlineData("eh", "eh", "some description", "new description", "description")]
	public async Task ExistingCommand_SingleEdit(
		string oldResponse,
		string newResponse,
		string oldDescription,
		string newDescription,
		string changedProperty)
	{
		const string commandName = "!test";

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = oldResponse,
			Description = oldDescription
		};

		_commands.Add(existingCommand);

		var description = newDescription != string.Empty ? $" <{newDescription}>" : null;
		var messageContent = $"{_target.Name} {commandName} {newResponse}{description}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Changed {changedProperty} of '{existingCommand.Name}'.", response);
		Assert.Equal(newResponse, existingCommand.Response);
		Assert.Equal(newDescription, existingCommand.Description);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Once);
	}

	[Fact]
	public async Task ExistingCommand_RemoveAllAlternateNames()
	{
		const string commandName = "!test";
		const string commandResponse = "someResponse";
		const string description = "someDescription";

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = commandResponse,
			Description = description,
			AlternateName = new List<string>
			{
				"!someAlt"
			}
		};

		_commands.Add(existingCommand);

		var messageContent = $"{_target.Name} {commandName} {commandResponse} <{description}>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Removed all alternate commands for '{existingCommand.Name}'", response);
		Assert.Null(existingCommand.AlternateName);
		Assert.Equal(commandName, existingCommand.Name);
		Assert.Equal(commandResponse, existingCommand.Response);
		Assert.Equal(description, existingCommand.Description);
		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Once);
	}

	[Fact]
	public async Task ExistingCommand_EditAlternateNames()
	{
		const string commandName = "!test";
		const string commandResponse = "someResponse";

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = commandResponse,
			Description = ""
		};

		_commands.Add(existingCommand);

		var alternateNames = new List<string>
		{
			"!someAlt",
			"!someOtherAlt"
		};

		var alternates = string.Join(" ", alternateNames);

		var messageContent = $"{_target.Name} {commandName} {alternates} {commandResponse}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Changed alternate command names for '{existingCommand.Name}'. It now has {alternateNames.Count} alternates.", response);
		Assert.NotNull(existingCommand.AlternateName);
		Assert.Equal(commandName, existingCommand.Name);
		Assert.Equal(commandResponse, existingCommand.Response);

		Assert.Equal(2, existingCommand.AlternateName!.Count);
		Assert.Equal(alternateNames[0], existingCommand.AlternateName[0]);
		Assert.Equal(alternateNames[1], existingCommand.AlternateName[1]);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Once);
	}

	[Fact]
	public async Task ExistingCommand_MultipleChanges()
	{
		const string commandName = "!test";

		const string oldCommandResponse = "someResponse";
		const string newCommandResponse = "someNewResponse";

		const string oldDescription = "someDescription";
		const string newDescription = "someNewDescription";

		const string existingAltName = "!oldAlt";

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = oldCommandResponse,
			Description = oldDescription,
			AlternateName = new List<string>
			{
				existingAltName
			}
		};

		_commands.Add(existingCommand);

		var newAlternateNames = new List<string>
		{
			"!someAlt",
			"!someOtherAlt"
		};

		var alternates = string.Join(" ", newAlternateNames);

		var messageContent = $"{_target.Name} {commandName} {alternates} {newCommandResponse} <{newDescription}>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Done. Several changes made to command '{existingCommand.Name}'.", response);
		Assert.NotNull(existingCommand.AlternateName);
		Assert.Equal(commandName, existingCommand.Name);
		Assert.Equal(newCommandResponse, existingCommand.Response);
		Assert.Equal(newDescription, existingCommand.Description);

		Assert.Equal(3, existingCommand.AlternateName!.Count);
		Assert.Equal(existingAltName, existingCommand.AlternateName[0]);
		Assert.Equal(newAlternateNames[0], existingCommand.AlternateName[1]);
		Assert.Equal(newAlternateNames[1], existingCommand.AlternateName[2]);

		_commandContainer.Verify(x => x.SaveCommandsToFile(), Times.Once);
	}
}
