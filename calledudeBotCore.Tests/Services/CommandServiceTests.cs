using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class CommandServiceTests
{
	private readonly IMessageBot<DiscordMessage> _botMock;
	private readonly Logger<CommandService<DiscordMessage>> _logger = new(NullLoggerFactory.Instance);

	public CommandServiceTests()
	{
		_botMock = Substitute.For<IMessageBot<DiscordMessage>>();

		_botMock
			.SendMessageAsync(Arg.Any<DiscordMessage>())
			.Returns(Task.CompletedTask);
	}

	[Fact]
	public async Task Invalid_Command_Error_Response()
	{
		var commandContainer = CommandContainerObjectMother.CreateLazy();
		var logger = new Logger<CommandService<DiscordMessage>>(NullLoggerFactory.Instance);

		var commandService = new CommandService<DiscordMessage>(logger, _botMock, commandContainer);

		var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent("!IDoesNotExist");
		await commandService.Handle(discordMessage, CancellationToken.None);

		const string expectedResponse = "Not sure what you were trying to do? That is not an available command. Try '!help' or '!help <command>'";
		await _botMock.Received(1).SendMessageAsync(Arg.Is<DiscordMessage>(x => x.Content == expectedResponse));
	}

	[Fact]
	public async Task NonModerator_Executing_ElevatedCommand_Errors()
	{
		const string commandName = "!whatever";
		var commandMock = Substitute.For<Command>();
		commandMock.RequiresMod.Returns(true);
		commandMock.Name.Returns(commandName);

		var commandContainer = CommandContainerObjectMother.CreateLazy(commandMock);

		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);
		var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent(commandName);

		await commandService.Handle(discordMessage, CancellationToken.None);

		await _botMock.Received(1).SendMessageAsync(Arg.Is<DiscordMessage>(x => x.Content == "You're not allowed to use that command"));
	}

	[Fact]
	public async Task Bot_Always_Responds_If_Valid_Command()
	{
		var cmd = new Command
		{
			Name = "!test",
			Response = "waddup"
		};

		var commandContainer = CommandContainerObjectMother.CreateLazy(cmd);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(cmd.Name), CancellationToken.None);

		await _botMock.Received(1).SendMessageAsync(Arg.Is<DiscordMessage>(x => x.Content == "waddup"));
	}

	[Fact]
	public async Task Bot_Never_Responds_On_Invalid_Command()
	{
		var commandContainer = CommandContainerObjectMother.CreateLazy();
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent("This is a regular message"), CancellationToken.None);

		await _botMock.DidNotReceive().SendMessageAsync(Arg.Any<DiscordMessage>());
	}

	[Fact]
	public async Task Bot_Responds_On_SpecialCommand_WithParameter()
	{
		const string commandName = "!special";
		const string response = "nice";
		var specialCommand = Substitute.For<SpecialCommand<CommandParameter>>();
		specialCommand.Name.Returns(commandName);

		// Not sure what I think about this
		// This got even uglier with NSubstitute :(
		specialCommand
			.GetType()
			.GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Instance)!
			.Invoke(specialCommand, new[] { Arg.Any<CommandParameter>() })
			.Returns(Task.FromResult(response));

		var commandContainer = CommandContainerObjectMother.CreateLazy(specialCommand);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(commandName), CancellationToken.None);

		await _botMock.Received(1).SendMessageAsync(Arg.Is<DiscordMessage>(x => x.Content == response));
	}

	[Fact]
	public async Task Bot_Responds_On_SpecialCommand_WithoutParameter()
	{
		const string commandName = "!special";
		const string response = "nice";
		var specialCommand = Substitute.For<SpecialCommand>();
		specialCommand.Name.Returns(commandName);
		specialCommand.Handle().Returns(response);

		var commandContainer = CommandContainerObjectMother.CreateLazy(specialCommand);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(commandName), CancellationToken.None);

		await _botMock.Received(1).SendMessageAsync(Arg.Is<DiscordMessage>(x => x.Content == response));
	}

	[Fact]
	public async Task Bot_Never_Responds_On_Invalid_Command_Configuration()
	{
		var command = new Command
		{
			Name = "!command"
		};

		var commandContainer = CommandContainerObjectMother.CreateLazy(command);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(command.Name), CancellationToken.None);

		await _botMock.DidNotReceive().SendMessageAsync(Arg.Any<DiscordMessage>());
	}
}