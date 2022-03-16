using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBotCore.Tests.ObjectMothers;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class DeleteCommandTests
{
	private readonly DeleteCommand _target;
	private readonly Mock<ICommandContainer> _commandContainer;
	private readonly Dictionary<string, Command> _commands = new();

	public DeleteCommandTests()
	{
		_commandContainer = new Mock<ICommandContainer>();
		_commandContainer.SetupGet(x => x.Commands).Returns(_commands);

		var lazyCommandContainer = new Lazy<ICommandContainer>(_commandContainer.Object);
		var (delete, _) = CommandContainerObjectMother.CreateWithSpecialCommand(_ => new DeleteCommand(lazyCommandContainer));
		_target = delete;
	}

	[Fact]
	public async Task DeletingSpecialCommandNotAllowed_SpecialCommand()
	=> await DeletingSpecialCommandNotAllowed<SpecialCommand>();

	[Fact]
	public async Task DeletingSpecialCommandNotAllowed_SpecialCommand_CommandParameter()
	=> await DeletingSpecialCommandNotAllowed<SpecialCommand<CommandParameter>>();

	private async Task DeletingSpecialCommandNotAllowed<T>() where T : Command
	{
		var specialCommand = new Mock<T>();
		specialCommand.SetupGet(x => x.Name).Returns("!something");
		_commands.Add(specialCommand.Object);

		var messageContent = $"{_target.Name} !something";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You can't remove a special command.", response);
	}

	[Fact]
	public async Task DeleteAlternate()
	{

		const string alternateName = "!yo";
		const string cmdName = "!hi";
		var commandToDelete = new Command()
		{
			Name = cmdName,
			Response = "hi :)",
			AlternateName = new List<string> { alternateName }
		};

		_commands.Add(commandToDelete);

		var messageParams = $"{_target.Name} {alternateName}".Split();
		var response = await _target.Handle(new CommandParameter<IrcMessage>(messageParams, new IrcMessage("", null, null)));

		Assert.DoesNotContain(_commands, x => x.Key == alternateName);
		Assert.Contains(_commands, x => x.Key == cmdName);
		Assert.Empty(commandToDelete.AlternateName);
		Assert.Equal($"Deleted alternative command '{alternateName}'", response);
	}

	[Fact]
	public async Task DeleteCommandAndAlternates()
	{
		const string alternateName = "!yo";
		const string cmdName = "!hi";
		var commandToDelete = new Command()
		{
			Name = cmdName,
			Response = "hi :)",
			AlternateName = new List<string> { alternateName }
		};

		_commands.Add(commandToDelete);
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{_target.Name} {commandToDelete.Name}");

		var response = await _target.Handle(commandParameter);

		Assert.DoesNotContain(_commands, x => x.Key == cmdName || x.Key == alternateName);
		Assert.Equal($"Deleted command '{commandToDelete.Name}'", response);
	}

	[Fact]
	public async Task DeleteCommand_UsingNonPrefixedWord()
	{
		const string cmdName = "!hi";
		var commandToDelete = new Command()
		{
			Name = cmdName,
			Response = "hi :)",
		};

		_commands.Add(commandToDelete);
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{_target.Name} hi");

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Deleted command '{commandToDelete.Name}'", response);
	}

	[Theory]
	[InlineData(" !doesNotExist")]
	[InlineData(null)]
	public async Task DeleteCommand_InvalidCommand(string commandToDelete)
	{
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{_target.Name}{commandToDelete}");

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You ok there bud? Try again.", response);
	}
}
