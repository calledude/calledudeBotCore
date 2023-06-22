using calledudeBot.Bots;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Discord;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DiscordMessage = calledudeBot.Chat.DiscordMessage;

namespace calledudeBotCore.Tests.Bots;
public class DiscordBotTests
{
	private readonly Mock<IDiscordSocketClient> _discordSocketClient;
	private readonly Mock<IMessageDispatcher> _messageDispatcher;
	private readonly Mock<ILogger<DiscordBot>> _logger;
	private readonly DiscordBot _discordBot;

	public DiscordBotTests()
	{
		_discordSocketClient = new Mock<IDiscordSocketClient>();
		_messageDispatcher = new Mock<IMessageDispatcher>();
		_logger = new Mock<ILogger<DiscordBot>>();

		_discordBot = new DiscordBot(
			_logger.Object,
			ConfigObjectMother.Create(),
			_discordSocketClient.Object,
			_messageDispatcher.Object
			);
	}

	[Fact]
	public async Task Not_SocketUserMessage_Bails()
	{
		Func<IMessage, Task>? messageReceivedEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.MessageReceived += It.IsAny<Func<IMessage, Task>>())
			.Callback((Func<IMessage, Task> evt) => messageReceivedEventSubscription = evt);

		await _discordBot.StartAsync(CancellationToken.None);

		await messageReceivedEventSubscription!.Invoke(new Mock<IMessage>().Object);

		_messageDispatcher.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task IsSelfUser_Bails()
	{
		Func<IMessage, Task>? messageReceivedEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.MessageReceived += It.IsAny<Func<IMessage, Task>>())
			.Callback((Func<IMessage, Task> evt) => messageReceivedEventSubscription = evt);

		await _discordBot.StartAsync(CancellationToken.None);

		var socketSelfUser = new Mock<ISelfUser>();
		socketSelfUser.Setup(x => x.Id).Returns(5);

		var messageMock = new Mock<IUserMessage>();
		messageMock.Setup(x => x.Author).Returns(socketSelfUser.Object);

		_discordSocketClient.Setup(x => x.CurrentUser).Returns(socketSelfUser.Object);

		await messageReceivedEventSubscription!.Invoke(messageMock.Object);

		_messageDispatcher.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task IsNotGuildUser_Bails()
	{
		Func<IMessage, Task>? messageReceivedEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.MessageReceived += It.IsAny<Func<IMessage, Task>>())
			.Callback((Func<IMessage, Task> evt) => messageReceivedEventSubscription = evt);

		await _discordBot.StartAsync(CancellationToken.None);

		var socketSelfUser = new Mock<ISelfUser>();
		socketSelfUser.Setup(x => x.Id).Returns(5);

		var messageMock = new Mock<IUserMessage>();
		messageMock.Setup(x => x.Author).Returns(new Mock<IUser>().Object);

		_discordSocketClient.Setup(x => x.CurrentUser).Returns(socketSelfUser.Object);

		await messageReceivedEventSubscription!.Invoke(messageMock.Object);

		_messageDispatcher.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task DispatchedMessageHasCorrectData()
	{
		Func<IMessage, Task>? messageReceivedEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.MessageReceived += It.IsAny<Func<IMessage, Task>>())
			.Callback((Func<IMessage, Task> evt) => messageReceivedEventSubscription = evt);

		DiscordMessage? actualMessage = null;
		_messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => actualMessage = (DiscordMessage)notification);

		await _discordBot.StartAsync(CancellationToken.None);

		var socketSelfUser = new Mock<ISelfUser>();
		socketSelfUser.Setup(x => x.Id).Returns(5);

		_discordSocketClient.Setup(x => x.CurrentUser).Returns(socketSelfUser.Object);

		const ulong channelId = 12345;
		const string channelName = "someChannel";
		var channelMock = new Mock<IMessageChannel>();
		channelMock.Setup(x => x.Name).Returns(channelName);
		channelMock.Setup(x => x.Id).Returns(channelId);

		const string username = "calledude";
		var userMock = new Mock<IGuildUser>();
		userMock.Setup(x => x.Username).Returns(username);

		const string content = "Hello! :D";
		var messageMock = new Mock<IUserMessage>();
		messageMock.Setup(x => x.Content).Returns(content);
		messageMock.Setup(x => x.Author).Returns(userMock.Object);
		messageMock.Setup(x => x.Channel).Returns(channelMock.Object);

		await messageReceivedEventSubscription!.Invoke(messageMock.Object);

		Assert.Equal(content, actualMessage!.Content);
		Assert.Equal($"#{channelName}", actualMessage!.Channel);
		Assert.Equal(username, actualMessage!.Sender!.Name);
		Assert.Equal(channelId, actualMessage!.Destination);

		_messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<DiscordMessage>(), It.IsAny<CancellationToken>()), Times.Once);
		_messageDispatcher.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task SendMessage_CallsCorrectMethods_WithCorrectArguments()
	{
		const string content = "hi! :)";
		const ulong channelId = 12345;

		var message = MessageObjectMother.CreateDiscordMessage(content, channelId);

		var messageChannelMock = new Mock<IMessageChannel>();
		_discordSocketClient
			.Setup(x => x.GetMessageChannel(It.IsAny<ulong>()))
			.Returns(messageChannelMock.Object);

		await _discordBot.SendMessageAsync(message);

		_discordSocketClient.Verify(x => x.GetMessageChannel(channelId), Times.Once);
		messageChannelMock
			.Verify(x =>
				x.SendMessageAsync(
					content,
					It.IsAny<bool>(),
					It.IsAny<Embed>(),
					It.IsAny<RequestOptions>(),
					It.IsAny<AllowedMentions>(),
					It.IsAny<MessageReference>(),
					It.IsAny<MessageComponent>(),
					It.IsAny<ISticker[]>(),
					It.IsAny<Embed[]>(),
					It.IsAny<MessageFlags>()
					), Times.Once);
	}

	[Fact]
	public async Task Stop_ShutsClientDown()
	{
		await _discordBot.StopAsync(CancellationToken.None);

		_discordSocketClient.Verify(x => x.Logout(), Times.Once);
		_discordSocketClient.Verify(x => x.Stop(), Times.Once);
	}

	[Fact]
	public async Task Ready_PublishesReadyNotification()
	{
		Func<Task>? readyEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.Ready += It.IsAny<Func<Task>>())
			.Callback((Func<Task> evt) => readyEventSubscription = evt);

		await _discordBot.StartAsync(CancellationToken.None);

		await readyEventSubscription!.Invoke();

		_messageDispatcher.Verify(x => x.PublishAsync(It.IsAny<ReadyNotification>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(true, true)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task GuildPermission_BanAndKick_CountsAsMod(bool banMembers, bool kickMembers)
	{
		Func<IMessage, Task>? messageReceivedEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.MessageReceived += It.IsAny<Func<IMessage, Task>>())
			.Callback((Func<IMessage, Task> evt) => messageReceivedEventSubscription = evt);

		DiscordMessage? actualMessage = null;
		_messageDispatcher
			.Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
			.Callback((INotification notification, CancellationToken _) => actualMessage = (DiscordMessage)notification);

		await _discordBot.StartAsync(CancellationToken.None);

		var socketSelfUser = new Mock<ISelfUser>();
		socketSelfUser.Setup(x => x.Id).Returns(5);

		_discordSocketClient.Setup(x => x.CurrentUser).Returns(socketSelfUser.Object);

		var channelMock = new Mock<IMessageChannel>();

		var guildPermissions = new GuildPermissions(kickMembers: kickMembers, banMembers: banMembers);
		var userMock = new Mock<IGuildUser>();
		userMock.Setup(x => x.GuildPermissions).Returns(guildPermissions);

		var messageMock = new Mock<IUserMessage>();
		messageMock.Setup(x => x.Author).Returns(userMock.Object);
		messageMock.Setup(x => x.Channel).Returns(channelMock.Object);

		await messageReceivedEventSubscription!.Invoke(messageMock.Object);
		var isMod = await actualMessage!.Sender!.IsModerator();

		Assert.True(isMod);
	}

	[Theory]
	[InlineData(LogSeverity.Critical, null)]
	[InlineData(LogSeverity.Critical, "")]
	[InlineData(LogSeverity.Debug, "")]
	[InlineData(LogSeverity.Warning, "")]
	[InlineData(LogSeverity.Error, null)]
	[InlineData(LogSeverity.Error, "")]
	[InlineData(LogSeverity.Info, "")]
	[InlineData(LogSeverity.Verbose, "")]
	public async Task Log_CallsCorrectMethod(LogSeverity severity, string message)
	{
		Func<LogMessage, Task>? logEventSubscription = null;
		_discordSocketClient
			.SetupAdd(x => x.Log += It.IsAny<Func<LogMessage, Task>>())
			.Callback((Func<LogMessage, Task> evt) => logEventSubscription = evt);

		await _discordBot.StartAsync(CancellationToken.None);

		await logEventSubscription!.Invoke(new LogMessage(severity, "", message));

		void VerifyLogCall(LogLevel logLevel)
		{
			_logger.Verify(x => x.Log(
				logLevel,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}

		switch (severity)
		{
			case LogSeverity.Critical:
				VerifyLogCall(LogLevel.Critical);
				break;
			case LogSeverity.Error:
				VerifyLogCall(LogLevel.Error);
				break;
			case LogSeverity.Warning:
				VerifyLogCall(LogLevel.Warning);
				break;
			case LogSeverity.Info:
				VerifyLogCall(LogLevel.Information);
				break;
			case LogSeverity.Verbose:
				VerifyLogCall(LogLevel.Trace);
				break;
			case LogSeverity.Debug:
				VerifyLogCall(LogLevel.Debug);
				break;
		}
	}
}