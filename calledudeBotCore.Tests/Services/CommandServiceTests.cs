using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests;

public class CommandServiceTests
{
	private readonly Mock<IMessageBot<DiscordMessage>> _botMock;
	private readonly Logger<CommandService<DiscordMessage>> _logger = new(NullLoggerFactory.Instance);

	public CommandServiceTests()
	{
		_botMock = new Mock<IMessageBot<DiscordMessage>>();

		_botMock
			.Setup(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()))
			.Returns(Task.CompletedTask)
			.Verifiable();
	}

	[Fact]
	public async Task Invalid_Command_Error_Response()
	{
		var commandContainer = CommandContainerObjectMother.CreateLazy();
		var logger = new Logger<CommandService<DiscordMessage>>(NullLoggerFactory.Instance);

		var commandService = new CommandService<DiscordMessage>(logger, _botMock.Object, commandContainer);

		var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent("!IDoesNotExist");
		await commandService.Handle(discordMessage, CancellationToken.None);

		const string expectedResponse = "Not sure what you were trying to do? That is not an available command. Try '!help' or '!help <command>'";
		_botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == expectedResponse)));
	}

	[Fact]
	public async Task NonModerator_Executing_ElevatedCommand_Errors()
	{
		const string commandName = "!whatever";
		var commandMock = new Mock<Command>();
		commandMock.SetupGet(x => x.RequiresMod).Returns(true);
		commandMock.SetupGet(x => x.Name).Returns(commandName);

		var commandContainer = CommandContainerObjectMother.CreateLazy(commandMock.Object);

		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);
		var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent(commandName);

		await commandService.Handle(discordMessage, CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "You're not allowed to use that command")));
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
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(cmd.Name), CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "waddup")));
	}

	[Fact]
	public async Task Bot_Never_Responds_On_Invalid_Command()
	{
		var commandContainer = CommandContainerObjectMother.CreateLazy();
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent("This is a regular message"), CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
	}

	[Fact]
	public async Task Bot_Responds_On_SpecialCommand_WithParameter()
	{
		const string commandName = "!special";
		const string response = "nice";
		var specialCommand = new Mock<SpecialCommand<CommandParameter>>();
		specialCommand
			.SetupGet(x => x.Name)
			.Returns(commandName);

		// Not sure what I think about this
		specialCommand
			.Protected()
			.Setup<Task<string>>("HandleCommand", ItExpr.IsAny<CommandParameter>())
			.Returns(Task.FromResult(response));

		var commandContainer = CommandContainerObjectMother.CreateLazy(specialCommand.Object);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(commandName), CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == response)));
	}

	[Fact]
	public async Task Bot_Responds_On_SpecialCommand_WithoutParameter()
	{
		const string commandName = "!special";
		const string response = "nice";
		var specialCommand = new Mock<SpecialCommand>();
		specialCommand
			.SetupGet(x => x.Name)
			.Returns(commandName);
		specialCommand
			.Setup(x => x.Handle())
			.ReturnsAsync(response);

		var commandContainer = CommandContainerObjectMother.CreateLazy(specialCommand.Object);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(commandName), CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == response)));
	}

	[Fact]
	public async Task Bot_Never_Responds_On_Invalid_Command_Configuration()
	{
		var command = new Command
		{
			Name = "!command"
		};

		var commandContainer = CommandContainerObjectMother.CreateLazy(command);
		var commandService = new CommandService<DiscordMessage>(_logger, _botMock.Object, commandContainer);

		await commandService.Handle(MessageObjectMother.CreateDiscordMessageWithContent(command.Name), CancellationToken.None);

		_botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
	}
}
