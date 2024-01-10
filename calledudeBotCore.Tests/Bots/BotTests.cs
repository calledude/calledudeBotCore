using calledudeBot.Bots;
using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Bots;

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
				var cmdMock = Substitute.For<SpecialCommand<CommandParameter>>();

				cmdMock.RequiresMod.Returns(true);

				cmdMock.Name.Returns("!" + x);

				return cmdMock;
			}).ToArray();

		var commandContainer = CommandContainerObjectMother.CreateLazy(fakeCommands);

		var botMock = Substitute.For<IMessageBot<IrcMessage>>();
		var twitchCommandService = new CommandService<IrcMessage>(_commandLogger, botMock, commandContainer);

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
		var twitch = new TwitchBot(Substitute.For<IIrcClient>(), new TwitchBotConfig { TwitchChannel = "#calledude" }, null!, _twitchLogger);
		await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: CALLEDUDE, calLEDuDeBoT");
		var mods = await twitch.GetMods();

		Assert.Equal(2, mods.Count);
		Assert.Contains("calledude", mods);
		Assert.Contains("calledudebot", mods);
	}

	[Fact]
	public async Task CanHandleMultipleMessagesSimultaneously()
	{
		var tcpClient = Substitute.For<ITcpClient>();
		tcpClient
			.ReadLineAsync(Arg.Any<CancellationToken>())
			.Returns(
				"this 366 is a success code",
				":someUser!someUser@someUser.tmi.twitch.tv PRIVMSG #calledude :!test",
				":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: someUser"
			);

		// TODO: This test doesn't really make sense anymore
		var workItemQueueService = Substitute.For<IWorkItemQueueService>();

		var ircClient = new IrcClient(_ircClientLogger, tcpClient, workItemQueueService);

		var messageDispatcher = Substitute.For<IMessageDispatcher>();
		var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, _twitchLogger);

		var isModeratorChecked = new ManualResetEventSlim(false);
		messageDispatcher
			.PublishAsync(Arg.Any<IrcMessage>(), Arg.Any<CancellationToken>())
			.Returns(async call =>
			{
				// TODO: INotification?
				var message = call.Arg<IrcMessage>();
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
		var ircClient = Substitute.For<IIrcClient>();
		var messageDispatcher = Substitute.For<IMessageDispatcher>();
		messageDispatcher
			.PublishAsync(Arg.Do<IrcMessage>(x => message = x), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, _twitchLogger);
		await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: BogusUser");
		await twitch.HandleMessage("hello", "calledude");

		var mods = await twitch.GetMods();
		Assert.DoesNotContain(mods, x => x == "calledude");
		Assert.NotNull(message);
		Assert.True(await message.Sender!.IsModerator());
	}

	[Theory]
	[InlineData(ParticipationType.Join)]
	[InlineData(ParticipationType.Leave)]
	public async Task UserParticipationEventIsRaised(ParticipationType participationType)
	{
		var ircClient = Substitute.For<IIrcClient>();
		var messageDispatcher = Substitute.For<IMessageDispatcher>();

		UserParticipationNotification? userParticipation = null;
		messageDispatcher
			.PublishAsync(Arg.Do<UserParticipationNotification>(x => userParticipation = x), Arg.Any<CancellationToken>())
			.ReturnsForAnyArgs(Task.CompletedTask);

		var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, _twitchLogger);
		await twitch.HandleUserParticipation("calledude", participationType, "");

		await messageDispatcher.Received(1).PublishAsync(Arg.Any<UserParticipationNotification>(), Arg.Any<CancellationToken>());

		var calls = messageDispatcher.ReceivedCalls();
		Assert.Single(calls);

		Assert.NotNull(userParticipation);
		Assert.Equal(participationType, userParticipation.ParticipationType);
		Assert.Equal("calledude", userParticipation.User.Name);
	}

	[Fact]
	public async Task OnReady_RegistersCapabilities_AndReadyNotification()
	{
		var ircClient = Substitute.For<IIrcClient>();
		var messageDispatcher = Substitute.For<IMessageDispatcher>();

		ReadyNotification? readyNotification = null;
		messageDispatcher
			.PublishAsync(Arg.Do<ReadyNotification>(x => readyNotification = x), Arg.Any<CancellationToken>())
			.ReturnsForAnyArgs(Task.CompletedTask);

		var twitch = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, null!);
		await twitch.OnReady();

		await messageDispatcher.Received(1).PublishAsync(Arg.Any<ReadyNotification>(), Arg.Any<CancellationToken>());
		var calls = messageDispatcher.ReceivedCalls();
		Assert.Single(calls);

		await ircClient.Received(1).WriteLine(Arg.Is<string>(y => y == "CAP REQ :twitch.tv/commands"));
		await ircClient.Received(1).WriteLine(Arg.Is<string>(y => y == "CAP REQ :twitch.tv/membership"));

		Assert.NotNull(readyNotification);
		Assert.Equal(twitch, readyNotification.Bot);
	}

	[Fact]
	public void UserJoinIsPublished()
	{
		var ircClient = Substitute.For<IIrcClient>();

		UserParticipationNotification? actualNotification = null;
		var messageDispatcher = Substitute.For<IMessageDispatcher>();
		messageDispatcher
			.PublishAsync(Arg.Do<UserParticipationNotification>(x => actualNotification = x), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		//Subscribes to Leave/Join events
		_ = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, _twitchLogger);

		const string username = "calledude";
		ircClient.ChatUserJoined += Raise.Event<Func<string, Task>>(username);

		Assert.NotNull(actualNotification);
		Assert.Equal(username, actualNotification.User.Name);
		Assert.Equal(ParticipationType.Join, actualNotification.ParticipationType);
		Assert.Equal(DateTime.UtcNow, actualNotification.When, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void UserLeaveIsPublished()
	{
		var ircClient = Substitute.For<IIrcClient>();

		UserParticipationNotification? actualNotification = null;
		var messageDispatcher = Substitute.For<IMessageDispatcher>();
		messageDispatcher
			.PublishAsync(Arg.Do<UserParticipationNotification>(x => actualNotification = x), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		//Subscribes to Leave/Join events
		_ = new TwitchBot(ircClient, new TwitchBotConfig { TwitchChannel = "#calledude" }, messageDispatcher, _twitchLogger);

		const string username = "calledude";
		ircClient.ChatUserLeft += Raise.Event<Func<string, Task>>(username);

		Assert.NotNull(actualNotification);
		Assert.Equal(username, actualNotification.User.Name);
		Assert.Equal(ParticipationType.Leave, actualNotification.ParticipationType);
		Assert.Equal(DateTime.UtcNow, actualNotification.When, TimeSpan.FromSeconds(1));
	}
}