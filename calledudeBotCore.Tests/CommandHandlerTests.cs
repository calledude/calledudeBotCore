using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests
{
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

            var commandContainer = new Lazy<CommandContainer>(() => new CommandContainer(Enumerable.Empty<Command>()));
            var logger = new Logger<CommandHandler<DiscordMessage>>(NullLoggerFactory.Instance);

            var commandHandler = new CommandHandler<DiscordMessage>(logger, botMock.Object, commandContainer);

            const string messageContent = "!IDoesNotExist";
            var messageParams = messageContent.Split();

            var discordMessage = new DiscordMessage(
                messageContent,
                "",
                new User(
                    "",
                    () => Task.FromResult(true)),
                0);

            var cmdParam = new CommandParameter<DiscordMessage>(messageParams, discordMessage);

            await commandHandler.Handle(discordMessage, CancellationToken.None);

            const string expectedResponse = "Not sure what you were trying to do? That is not an available command. Try '!help' or '!help <command>'";
            botMock
                .Verify(x => x.SendMessageAsync(
                    It.Is<DiscordMessage>(x => x.Content == expectedResponse)));
        }

        [Fact]
        public async Task NonModerator_Executing_ElevatedCommand_Errors()
        {
            var botMock = new Mock<IMessageBot<DiscordMessage>>();
            botMock.Setup(x => x.SendMessageAsync(It.IsAny<DiscordMessage>())).Verifiable();

            var commandContainer = new Lazy<CommandContainer>(() => new CommandContainer(Enumerable.Empty<Command>()));
            var addCmd = new AddCommand(commandContainer);
            commandContainer.Value.Commands.Add(addCmd);

            var logger = new Logger<CommandHandler<DiscordMessage>>(NullLoggerFactory.Instance);

            var commandHandler = new CommandHandler<DiscordMessage>(logger, botMock.Object, commandContainer);

            const string messageContent = "!addcmd !test nah fam <nice>";

            var discordMessage = new DiscordMessage(
                messageContent,
                "",
                new User("", false),
                0);

            await commandHandler.Handle(discordMessage, CancellationToken.None);

            botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "You're not allowed to use that command")));
            //Assert.Equal("You're not allowed to use that command", responseMessage.Content);
            Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Value.Name == "!test");
        }

        [Fact]
        public async Task Bot_Always_Responds_If_Valid_Command()
        {
            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(Enumerable.Empty<Command>()));

            var cmd = new Command
            {
                Name = "!test",
                Response = "waddup"
            };

            commandContainer.Value.Commands.Add(cmd);

            var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

            await commandHandler.Handle(
                new DiscordMessage(
                    "!test",
                    "",
                    new User("", false),
                    0), CancellationToken.None);

            _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "waddup")));
        }

        [Fact]
        public async Task Bot_Never_Responds_On_Invalid_Command()
        {
            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(Enumerable.Empty<Command>()));
            var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

            await commandHandler.Handle(
                new DiscordMessage(
                    "This is a regular message",
                    "",
                    new User("", false),
                    0), CancellationToken.None);

            _botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
        }

        [Fact]
        public async Task Bot_Responds_On_SpecialCommand_WithParameter()
        {
            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(Enumerable.Empty<Command>()));
            var help = new HelpCommand(commandContainer);

            commandContainer.Value.Commands.Add(help);

            var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

            await commandHandler.Handle(
                new DiscordMessage(
                    "!help",
                    "",
                    new User("", false),
                    0), CancellationToken.None);

            _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "These are the commands you can use: !help")));
        }

        [Fact]
        public async Task Bot_Responds_On_SpecialCommand_WithoutParameter()
        {
            var uptime = new UptimeCommand(new Mock<IStreamMonitor>().Object);
            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(Enumerable.Empty<Command>()));

            commandContainer.Value.Commands.Add(uptime);

            var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

            await commandHandler.Handle(
                new DiscordMessage(
                    "!uptime",
                    "",
                    new User("", false),
                    0), CancellationToken.None);

            _botMock.Verify(x => x.SendMessageAsync(It.Is<DiscordMessage>(x => x.Content == "Streamer isn't live.")));
        }

        [Fact]
        public async Task Bot_Never_Responds_On_Invalid_Command_Configuration()
        {
            var command = new Command
            {
                Name = "!command"
            };

            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(Enumerable.Empty<Command>()));

            commandContainer.Value.Commands.Add(command);

            var commandHandler = new CommandHandler<DiscordMessage>(_logger, _botMock.Object, commandContainer);

            await commandHandler.Handle(
                new DiscordMessage(
                    "!command",
                    "",
                    new User("", false),
                    0), CancellationToken.None);

            _botMock.Verify(x => x.SendMessageAsync(It.IsAny<DiscordMessage>()), Times.Never);
        }
    }
}
