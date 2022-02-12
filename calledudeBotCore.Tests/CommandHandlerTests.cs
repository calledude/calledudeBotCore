using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests;

public class CommandHandlerTests
{
    private readonly Mock<IMessageBot<DiscordMessage>> _botMock;
    private readonly Logger<CommandHandler<DiscordMessage>> _logger = new(NullLoggerFactory.Instance);

    public CommandHandlerTests()
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
        var botMock = new Mock<IMessageBot<DiscordMessage>>();
        botMock.Setup(x => x.SendMessageAsync(It.IsAny<DiscordMessage>())).Verifiable();

        var commandContainer = CommandContainerObjectMother.CreateLazy();
        var logger = new Logger<CommandHandler<DiscordMessage>>(NullLoggerFactory.Instance);

        var commandHandler = new CommandHandler<DiscordMessage>(logger, botMock.Object, commandContainer);

        var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent("!IDoesNotExist");
        await commandHandler.Handle(discordMessage, CancellationToken.None);

        const string expectedResponse = "Not sure what you were trying to do? That is not an available command. Try '!help' or '!help <command>'";
        botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == expectedResponse)));
    }

    [Fact]
    public async Task NonModerator_Executing_ElevatedCommand_Errors()
    {
        var botMock = new Mock<IMessageBot<DiscordMessage>>();
        botMock.Setup(x => x.SendMessageAsync(It.IsAny<DiscordMessage>())).Verifiable();

        var (add, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new AddCommand(container));

        var commandHandler = new CommandHandler<DiscordMessage>(_logger, botMock.Object, commandContainer);
        var discordMessage = MessageObjectMother.CreateDiscordMessageWithContent($"{add.Name} !test nah fam <nice>");

        await commandHandler.Handle(discordMessage, CancellationToken.None);

        botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "You're not allowed to use that command")));
        Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Value.Name == "!test");
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
        var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

        await commandHandler.Handle(MessageObjectMother.CreateDiscordMessageWithContent(cmd.Name), CancellationToken.None);

        _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "waddup")));
    }

    [Fact]
    public async Task Bot_Never_Responds_On_Invalid_Command()
    {
        var commandContainer = CommandContainerObjectMother.CreateLazy();
        var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

        await commandHandler.Handle(MessageObjectMother.CreateDiscordMessageWithContent("This is a regular message"), CancellationToken.None);

        _botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
    }

    [Fact]
    public async Task Bot_Responds_On_SpecialCommand_WithParameter()
    {
        var (help, commandContainer) = CommandContainerObjectMother.CreateWithSpecialCommand((container) => new HelpCommand(container));
        var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

        await commandHandler.Handle(MessageObjectMother.CreateDiscordMessageWithContent(help.Name), CancellationToken.None);

        _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "These are the commands you can use: !help")));
    }

    [Fact]
    public async Task Bot_Responds_On_SpecialCommand_WithoutParameter()
    {
        var uptime = new UptimeCommand(new Mock<IStreamMonitor>().Object);
        var commandContainer = CommandContainerObjectMother.CreateLazy(uptime);
        var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

        await commandHandler.Handle(MessageObjectMother.CreateDiscordMessageWithContent(uptime.Name), CancellationToken.None);

        _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "Streamer isn't live.")));
    }

    [Fact]
    public async Task Bot_Never_Responds_On_Invalid_Command_Configuration()
    {
        var command = new Command
        {
            Name = "!command"
        };

        var commandContainer = CommandContainerObjectMother.CreateLazy(command);
        var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

        await commandHandler.Handle(MessageObjectMother.CreateDiscordMessageWithContent(command.Name), CancellationToken.None);

        _botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
    }
}
