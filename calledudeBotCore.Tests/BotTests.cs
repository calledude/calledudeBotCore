using calledudeBot.Bots;
using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Config;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests;

public class BotTests
{
    private static readonly Logger<CommandHandler<IrcMessage>> _commandLogger = new(NullLoggerFactory.Instance);
    private static readonly Logger<IrcClient> _ircClientLogger = new(NullLoggerFactory.Instance);
    private static readonly Logger<TwitchBot> _twitchLogger = new(NullLoggerFactory.Instance);

    [Fact]
    public async Task Mods_Only_Evaluates_Once_Per_Context()
    {
        var isModInvokeCount = 0;

        var fakeCommands = Enumerable
            .Range(1, 10)
            .Select(x =>
            {
                var cmdMock = new Mock<SpecialCommand<CommandParameter>>();

                cmdMock
                    .SetupGet(x => x.RequiresMod)
                    .Returns(true);

                cmdMock
                    .SetupGet(x => x.Name)
                    .Returns("!" + x);

                return cmdMock.Object;
            }).ToArray();

        var commandContainer = CommandContainerObjectMother.CreateLazy(fakeCommands);

        var botMock = new Mock<IMessageBot<IrcMessage>>();
        var twitchCommandHandler = new CommandHandler<IrcMessage>(_commandLogger, botMock.Object, commandContainer);

        var message = new IrcMessage(
            "",
            "",
            new User("", () =>
            {
                isModInvokeCount++;
                return Task.FromResult(true);
            }));

        var commandExecutions = fakeCommands.Select(x =>
        {
            message = message.CloneWithMessage(x.Name);
            return twitchCommandHandler.Handle(message, CancellationToken.None);
        });

        await Task.WhenAll(commandExecutions);

        Assert.Equal(1, isModInvokeCount);
    }

    [Fact]
    public async Task Mods_Are_Case_Insensitive()
    {
        var ircClient = new IrcClient(_ircClientLogger, new Mock<ITcpClient>().Object);
        var twitch = new TwitchBot(ircClient, new TwitchBotConfig() { TwitchChannel = "#calledude" }, new Mock<IMessageDispatcher>().Object, _twitchLogger);
        await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: CALLEDUDE, calLEDuDeBoT");
        var mods = await twitch.GetMods();

        Assert.Equal(2, mods.Count);
        Assert.Contains("calledude", mods);
        Assert.Contains("calledudebot", mods);
    }

    [Fact]
    public async Task CanHandleMultipleMessagesSimultaneously()
    {
        var lastLineRead = false;
        var tcpClient = new Mock<ITcpClient>();
        tcpClient
            .SetupSequence(x => x.ReadLineAsync())
            .ReturnsAsync("this 366 is a success code")
            .ReturnsAsync(":someUser!someUser@someUser.tmi.twitch.tv PRIVMSG #calledude :!test")
            .ReturnsAsync(() =>
            {
                lastLineRead = true;
                return ":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: someUser";
            });

        var ircClient = new IrcClient(_ircClientLogger, tcpClient.Object);

        var messageDispatcher = new Mock<IMessageDispatcher>();
        var twitch = new TwitchBot(ircClient, new TwitchBotConfig() { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);

        var isModCalled = false;
        messageDispatcher.Setup(x => x.PublishAsync(It.IsAny<IrcMessage>()))
            .Returns(async (INotification notification) =>
            {
                var message = (IrcMessage)notification;
                await message.Sender.IsModerator();
                isModCalled = true;
            });

        await twitch.StartAsync(CancellationToken.None);

        for (var tries = 0; tries < 5 && !isModCalled && !lastLineRead; tries++)
        {
            await Task.Delay(50);
        }

        Assert.True(lastLineRead);
        Assert.True(isModCalled);
    }
}
