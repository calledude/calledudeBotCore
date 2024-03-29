﻿using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBotCore.Tests.ObjectMothers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class DeleteCommandTests
{
	private readonly DeleteCommand _target;
	private readonly ICommandContainer _commandContainer;
	private readonly Dictionary<string, Command> _commands = [];

	public DeleteCommandTests()
	{
		_commandContainer = Substitute.For<ICommandContainer>();
		_commandContainer.Commands.Returns(_commands);

		var lazyCommandContainer = new Lazy<ICommandContainer>(_commandContainer);
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
		var specialCommand = Substitute.For<T>();
		specialCommand.Name.Returns("!something");
		_commands.Add(specialCommand);

		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod($"{_target.Name} !something");
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
			AlternateName = [alternateName]
		};

		_commands.Add(commandToDelete);

		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{_target.Name} {alternateName}");
		var response = await _target.Handle(commandParameter);

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
			AlternateName = [alternateName]
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
	public async Task DeleteCommand_InvalidCommand(string? commandToDelete)
	{
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContent($"{_target.Name}{commandToDelete}");

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You ok there bud? Try again.", response);
	}
}