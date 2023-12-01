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
    private readonly Dictionary<string, Command> _commands = new();
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
            AlternateName = new List<string> { "!old", "!one", "!two" }
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
            AlternateName = new List<string> { "!old", "!one", "!two" }
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

    [Fact] //TODO: alt?
    public async Task EditCommandAlternateNames_SuccessfulClear()
    {
        const string commandName = "!someExistingCommand";

        var existingCommand = new Command()
        {
            Name = commandName,
            AlternateName = new List<string> { "!old", "!one", "!two" }
        };

        _commands.Add(existingCommand);

        var messageContent = $"!edit {commandName} alternate clear";
        var commandParameter = CommandParameterObjectMother.CreateWithMessageContentAsMod(messageContent);
        var result = await _target.Handle(commandParameter);

        Assert.Equal($"Cleared all alternative names for {commandName}", result);
        Assert.Empty(existingCommand.AlternateName);
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

        _commandContainer.DidNotReceive().SaveCommandsToFile();
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

        _commandContainer.Received(1).SaveCommandsToFile();
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
        _commandContainer.Received(1).SaveCommandsToFile();
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

        _commandContainer.Received(1).SaveCommandsToFile();
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

        Assert.Equal("You can't change a special command.", response);
    }
}
