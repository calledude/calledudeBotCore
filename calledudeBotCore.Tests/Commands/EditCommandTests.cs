using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBotCore.Tests.ObjectMothers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;
public class EditCommandTests
{
	private readonly Dictionary<string, Command> _commands = [];
	private readonly ICommandContainer _commandContainer;
	private readonly EditCommand _target;

	public EditCommandTests()
	{
		_commandContainer = Substitute.For<ICommandContainer>();
		_commandContainer.Commands.Returns(_commands);

		var lazyCommandContainer = new Lazy<ICommandContainer>(_commandContainer);
		var (add, _) = CommandContainerObjectMother.CreateWithSpecialCommand(_ => new EditCommand(lazyCommandContainer));
		_target = add;
	}

	[Fact]
	public async Task EditCommandName_SuccessfulChange()
	{
		const string previousName = "!someExistingCommand";
		const string newName = "!theNewCommandName";

		var existingCommand = new Command()
		{
			Name = previousName,
		};

		_commands.Add(existingCommand);

		const string messageContent = $"!edit {previousName} name {newName}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
		var result = await _target.Handle(commandParameter);

		Assert.Equal($"Changed name of '{previousName}' to '{newName}'.", result);
		Assert.Equal(newName, existingCommand.Name);
	}

	[Fact] //TODO: desc|descr?
	public async Task EditCommandDescription_SuccessfulChange()
	{
		const string commandName = "!someExistingCommand";
		const string oldDescription = "old description";
		const string newDescription = "new description";

		var existingCommand = new Command()
		{
			Name = commandName,
			Description = oldDescription
		};

		_commands.Add(existingCommand);

		const string messageContent = $"!edit {commandName} description {newDescription}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
		var result = await _target.Handle(commandParameter);

		Assert.Equal($"Changed description of '{commandName}' to '{newDescription}'.", result);
		Assert.Equal(newDescription, existingCommand.Description);
	}

	[Fact] //TODO: resp?
	public async Task EditCommandResponse_SuccessfulChange()
	{
		const string commandName = "!someExistingCommand";
		const string oldResponse = "old response";
		const string newResponse = "new response";

		var existingCommand = new Command()
		{
			Name = commandName,
			Response = oldResponse
		};

		_commands.Add(existingCommand);

		const string messageContent = $"!edit {commandName} response {newResponse}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
		var result = await _target.Handle(commandParameter);

		Assert.Equal($"Changed response of '{commandName}' to '{newResponse}'.", result);
		Assert.Equal(newResponse, existingCommand.Response);
	}

	[Fact] //TODO: alt?
	public async Task EditCommandAlternateNames_SuccessfulAdd()
	{
		const string commandName = "!someExistingCommand";

		var existingCommand = new Command()
		{
			Name = commandName,
			AlternateName = ["!old"]
		};

		_commands.Add(existingCommand);

		var alternatesToAdd = new[] { "!one", "!two" };
		var messageContent = $"!edit {commandName} alternate add {string.Join(" ", alternatesToAdd)}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
		var result = await _target.Handle(commandParameter);

		var expectedNewAlternates = new[] { "!old", "!one", "!two" };
		Assert.Equal($"Added alternate names for '{commandName}' - {string.Join(" » ", alternatesToAdd)}", result);
		Assert.Equal(expectedNewAlternates, existingCommand.AlternateName);
	}

	[Fact] //TODO: alt?
	public async Task EditCommandAlternateNames_SuccessfulRemove()
	{
		const string commandName = "!someExistingCommand";

		var existingCommand = new Command()
		{
			Name = commandName,
			AlternateName = ["!old", "!one", "!two"]
		};

		_commands.Add(existingCommand);

		var alternatesToDelete = new[] { "!one", "!two" };
		var messageContent = $"!edit {commandName} alternate remove {string.Join(" ", alternatesToDelete)}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
		var result = await _target.Handle(commandParameter);

		var expectedNewAlternates = new[] { "!old" };
		Assert.Equal($"Removed alternate names for '{commandName}' - {string.Join(" » ", alternatesToDelete)}", result);
		Assert.Equal(expectedNewAlternates, existingCommand.AlternateName);
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
		var messageContent = $"{_target.Name} {commandName} alternate {alternates}";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("Nothing changed", response);

		_commandContainer.DidNotReceive().SaveCommandsToFile();
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
			AlternateName =
			[
				"!someAlt"
			]
		};

		_commands.Add(existingCommand);

		var messageContent = $"{_target.Name} {commandName} alternate clear";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal($"Cleared all alternative names for '{existingCommand.Name}'", response);
		Assert.Empty(existingCommand.AlternateName);
		Assert.Equal(commandName, existingCommand.Name);
		Assert.Equal(commandResponse, existingCommand.Response);
		Assert.Equal(description, existingCommand.Description);
		_commandContainer.Received(1).SaveCommandsToFile();
	}

	[Fact]
	public async Task EditingSpecialCommandNotAllowed_SpecialCommand()
		=> await EditingSpecialCommandNotAllowed<SpecialCommand>();

	[Fact]
	public async Task EditingSpecialCommandNotAllowed_SpecialCommand_CommandParameter()
		=> await EditingSpecialCommandNotAllowed<SpecialCommand<CommandParameter>>();

	private async Task EditingSpecialCommandNotAllowed<T>() where T : Command
	{
		var specialCommand = Substitute.For<T>();
		specialCommand.Name.Returns("!something");
		_commands.Add(specialCommand);

		var messageContent = $"{_target.Name} !something word";
		var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);

		var response = await _target.Handle(commandParameter);

		Assert.Equal("You can't edit a special command.", response);
	}
}