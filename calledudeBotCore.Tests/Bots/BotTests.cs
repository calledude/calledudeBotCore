using calledudeBot.Bots;
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
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests;

public class BotTests
{
	private static readonly Logger<CommandService<IrcMessage>> _commandLogger = LoggerObjectMother.NullLoggerFor<CommandService<IrcMessage>>();
	private static readonly Logger<IrcClient> _ircClientLogger = LoggerObjectMother.NullLoggerFor<IrcClient>();
	private static readonly Logger<TwitchBot> _twitchLogger = LoggerObjectMother.NullLoggerFor<TwitchBot>();

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
		var twitchCommandService = new CommandService<IrcMessage>(_commandLogger, botMock.Object, commandContainer);

		var message = new IrcMessage
		{
			Content = "",
			Sender = new User("", () =>
			{
				isModInvokeCount++;
				return Task.FromResult(true);
			})
		};

		var commandExecutions = fakeCommands.Select(x =>
		{
			message = message with { Content = x.Name! };
			return twitchCommandService.Handle(message, CancellationToken.None);
		});

		await Task.WhenAll(commandExecutions);

		Assert.Equal(1, isModInvokeCount);
	}

	[Fact]
	public async Task Mods_Are_Case_Insensitive()
	{
		var twitch = new TwitchBot(new Mock<IIrcClient>().Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, null!, _twitchLogger);
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

		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<IrcMessage>(), It.IsAny<CancellationToken>()))
			.Returns(async (INotification notification, CancellationToken _) =>
			{
				var message = (IrcMessage)notification;
				await message.Sender!.IsModerator();
				isModeratorChecked.Set();
			});

		await twitch.StartAsync(CancellationToken.None);

		var modSuccessfullyRead = isModeratorChecked.Wait(500);
		Assert.True(modSuccessfullyRead);
	}

	[Fact]
	public async Task BroadcasterIsMod()
	{
		IrcMessage? message = null;
		var ircClient = new Mock<IIrcClient>();
		var messageDispatcher = new Mock<IMessageDispatcher>();
		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<IrcMessage>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => message = (IrcMessage)notification);

		var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);
		await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: BogusUser");
		await twitch.HandleMessage("hello", "calledude");

		var mods = await twitch.GetMods();
		Assert.DoesNotContain(mods, x => x == "calledude");
		Assert.NotNull(message);
		Assert.True(await message!.Sender!.IsModerator());
	}

	[Theory]
	[InlineData(ParticipationType.Join)]
	[InlineData(ParticipationType.Leave)]
	public async Task UserParticipationEventIsRaised(ParticipationType participationType)
	{
		var ircClient = new Mock<IIrcClient>();
		var messageDispatcher = new Mock<IMessageDispatcher>();

		UserParticipationNotification? userParticipation = null;
		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<UserParticipationNotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => userParticipation = (UserParticipationNotification)notification);

		var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);
		await twitch.HandleUserParticipation("calledude", participationType, "");

		messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<UserParticipationNotification>(), It.IsAny<CancellationToken>()), Times.Once);
		messageDispatcher.VerifyNoOtherCalls();

		Assert.NotNull(userParticipation);
		Assert.Equal(participationType, userParticipation!.ParticipationType);
		Assert.Equal("calledude", userParticipation.User.Name);
	}

	[Fact]
	public async Task OnReady_RegistersCapabilities_AndReadyNotification()
	{
		var ircClient = new Mock<IIrcClient>();
		var messageDispatcher = new Mock<IMessageDispatcher>();

		ReadyNotification? readyNotification = null;
		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<ReadyNotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => readyNotification = (ReadyNotification)notification);

		var twitch = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, null!);
		await twitch.OnReady();

		messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<ReadyNotification>(), It.IsAny<CancellationToken>()), Times.Once);
		messageDispatcher.VerifyNoOtherCalls();

		ircClient.Verify(x => x.WriteLine(It.Is<string>(y => y == "CAP REQ :twitch.tv/commands")), Times.Once);
		ircClient.Verify(x => x.WriteLine(It.Is<string>(y => y == "CAP REQ :twitch.tv/membership")), Times.Once);

		Assert.NotNull(readyNotification);
		Assert.Equal(twitch, readyNotification!.Bot);
	}

	[Fact]
	public async Task UserJoinIsPublished()
	{
		Func<string, Task>? userJoinedEventSubscription = null;
		var ircClient = new Mock<IIrcClient>();
		ircClient
			.SetupAdd(x => x.ChatUserJoined += It.IsAny<Func<string, Task>>())
			.Callback((Func<string, Task> evt) => userJoinedEventSubscription = evt);

		UserParticipationNotification? actualNotification = null;
		var messageDispatcher = new Mock<IMessageDispatcher>();
		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<UserParticipationNotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => actualNotification = (UserParticipationNotification)notification);

		//Subscribes to Leave/Join events
		_ = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);

		const string username = "calledude";
		await userJoinedEventSubscription!.Invoke(username);

		Assert.Equal(username, actualNotification!.User.Name);
		Assert.Equal(ParticipationType.Join, actualNotification!.ParticipationType);
		Assert.Equal(DateTime.Now, actualNotification!.When, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task UserLeaveIsPublished()
	{
		Func<string, Task>? userLeftEventSubscription = null;
		var ircClient = new Mock<IIrcClient>();
		ircClient
			.SetupAdd(x => x.ChatUserLeft += It.IsAny<Func<string, Task>>())
			.Callback((Func<string, Task> evt) => userLeftEventSubscription = evt);

		UserParticipationNotification? actualNotification = null;
		var messageDispatcher = new Mock<IMessageDispatcher>();
		messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<UserParticipationNotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => actualNotification = (UserParticipationNotification)notification);

		//Subscribes to Leave/Join events
		_ = new TwitchBot(ircClient.Object, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher.Object, _twitchLogger);

		const string username = "calledude";
		await userLeftEventSubscription!.Invoke(username);

		Assert.Equal(username, actualNotification!.User.Name);
		Assert.Equal(ParticipationType.Leave, actualNotification!.ParticipationType);
		Assert.Equal(DateTime.Now, actualNotification!.When, TimeSpan.FromSeconds(1));
	}
}
