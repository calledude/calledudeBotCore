using calledudeBot.Chat.Commands;
using calledudeBotCore.Tests.ObjectMothers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class AddCommandTests
{
	private readonly AddCommand _target;
	private readonly ICommandContainer _commandContainer;
	private readonly Dictionary<string, Command> _commands = [];

	public AddCommandTests()
	{
		_commandContainer = Substitute.For<ICommandContainer>();
		_commandContainer.Commands.Returns(_commands);

		var lazyCommandContainer = new Lazy<ICommandContainer>(_commandContainer);
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

		Assert.Empty(_commandContainer.ReceivedCalls());
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

		_commandContainer.Received(1).SaveCommandsToFile();
	}

	[Fact]
	public async Task NewCommand_ConflictingCommandName()
	{
		const string conflictingName = "!test";
		const string commandName = "!somethingElse";
		_commands.Add(new Command
		{
			Name = "!somethingElse",
			AlternateName =
			[
				conflictingName
			]
		});

		var messageContent = $"{_target.Name} {conflictingName} nah fam <nice>";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Conflicting command name usage found in command '{commandName}'", response);

		_commandContainer.DidNotReceive().SaveCommandsToFile();
	}
}