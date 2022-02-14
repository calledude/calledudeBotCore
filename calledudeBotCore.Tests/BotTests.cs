﻿using calledudeBot.Bots;
using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Config;
using calledudeBot.Models;
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
            .Range(1, 5)
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
        var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, new Mock<IMessageDispatcher>().Object, _twitchLogger);
        await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: CALLEDUDE, calLEDuDeBoT");
        var mods = await twitch.GetMods();

        Assert.Equal(2, mods.Count);
        Assert.Contains("calledude", mods);
        Assert.Contains("calledudebot", mods);
    }

    [Fact]
    public async Task CanHandleMultipleMessagesSimultaneously()
    {
        var tcpClient = new Mock<ITcpClient>();
        tcpClient
            .SetupSequence(x => x.ReadLineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("this 366 is a success code")
            .ReturnsAsync(":someUser!someUser@someUser.tmi.twitch.tv PRIVMSG #calledude :!test")
            .ReturnsAsync(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: someUser");

        var ircClient = new IrcClient(_ircClientLogger, tcpClient.Object);

        var messageDispatcher = new Mock<IMessageDispatcher>();
        var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);

        var isModeratorChecked = new ManualResetEventSlim(false);

        messageDispatcher.Setup(x => x.PublishAsync(It.IsAny<IrcMessage>()))
            .Returns(async (INotification notification) =>
            {
                var message = (IrcMessage)notification;
                await message.Sender.IsModerator();
                isModeratorChecked.Set();
            });

        await twitch.StartAsync(CancellationToken.None);

        var modSuccessfullyRead = isModeratorChecked.Wait(500);
        Assert.True(modSuccessfullyRead);
    }

    [Fact]
    public async Task BroadcasterIsMod()
    {
        IrcMessage message = null;
        var ircClient = new Mock<IIrcClient>();
        var messageDispatcher = new Mock<IMessageDispatcher>();
        messageDispatcher
            .Setup(x => x.PublishAsync(It.IsAny<IrcMessage>()))
            .Callback((INotification notification) =>
            {
                message = (IrcMessage)notification;
            });

        var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);
        await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: BogusUser");
        await twitch.HandleMessage("hello", "calledude");

        var mods = await twitch.GetMods();
        Assert.DoesNotContain(mods, x => x == "calledude");
        Assert.True(await message.Sender.IsModerator());
    }

    [Theory]
    [InlineData(ParticipationType.Join)]
    [InlineData(ParticipationType.Leave)]
    public async Task UserParticipationEventIsRaised(ParticipationType participationType)
    {
        var ircClient = new Mock<IIrcClient>();
        var messageDispatcher = new Mock<IMessageDispatcher>();

        UserParticipationNotification userParticipation = null;
        messageDispatcher
            .Setup(x => x.PublishAsync(It.IsAny<UserParticipationNotification>()))
            .Callback((INotification notification) => userParticipation = (UserParticipationNotification)notification);

        var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);
        await twitch.HandleUserParticipation("calledude", participationType, "");

        messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<UserParticipationNotification>()), Times.Once);
        messageDispatcher.VerifyNoOtherCalls();

        Assert.Equal(participationType, userParticipation.ParticipationType);
        Assert.Equal("calledude", userParticipation.User.Name);
    }

    [Fact]
    public async Task OnReady_RegistersCapabilities_AndReadyNotification()
    {
        var ircClient = new Mock<IIrcClient>();
        var messageDispatcher = new Mock<IMessageDispatcher>();

        ReadyNotification readyNotification = null;
        messageDispatcher
            .Setup(x => x.PublishAsync(It.IsAny<ReadyNotification>()))
            .Callback((INotification notification) => readyNotification = (ReadyNotification)notification);

        var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, null);
        await twitch.OnReady();

        messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<ReadyNotification>()), Times.Once);
        messageDispatcher.VerifyNoOtherCalls();

        ircClient.Verify(x => x.WriteLine(It.Is<string>(y => y == "CAP REQ :twitch.tv/commands")), Times.Once);
        ircClient.Verify(x => x.WriteLine(It.Is<string>(y => y == "CAP REQ :twitch.tv/membership")), Times.Once);

        Assert.Equal(twitch, readyNotification.Bot);
    }
}
