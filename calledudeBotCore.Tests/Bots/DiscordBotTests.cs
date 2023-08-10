using calledudeBot.Bots;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Discord;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DiscordMessage = calledudeBot.Chat.DiscordMessage;

namespace calledudeBotCore.Tests.Bots;
public class DiscordBotTests
{
    private readonly IDiscordSocketClient _discordSocketClient;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ILogger<DiscordBot> _logger;
    private readonly DiscordBot _discordBot;

    public DiscordBotTests()
    {
        _discordSocketClient = Substitute.For<IDiscordSocketClient>();
        _messageDispatcher = Substitute.For<IMessageDispatcher>();
        _logger = Substitute.For<ILogger<DiscordBot>>();

        _discordBot = new DiscordBot(
            _logger,
            ConfigObjectMother.Create(),
            _discordSocketClient,
            _messageDispatcher
            );
    }

    [Fact]
    public async Task Not_SocketUserMessage_Bails()
    {
        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.MessageReceived += Raise.Event<Func<IMessage, Task>>(Substitute.For<IMessage>());

        Assert.Empty(_messageDispatcher.ReceivedCalls());
    }

    [Fact]
    public async Task IsSelfUser_Bails()
    {
        var socketSelfUser = Substitute.For<ISelfUser>();
        socketSelfUser.Id.Returns(5ul);

        var messageMock = Substitute.For<IUserMessage>();
        messageMock.Author.Returns(socketSelfUser);

        _discordSocketClient.CurrentUser.Returns(socketSelfUser);

        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.MessageReceived += Raise.Event<Func<IMessage, Task>>(messageMock);

        Assert.Empty(_messageDispatcher.ReceivedCalls());
    }

    [Fact]
    public async Task IsNotGuildUser_Bails()
    {
        var socketSelfUser = Substitute.For<ISelfUser>();
        socketSelfUser.Id.Returns(5ul);

        var messageMock = Substitute.For<IUserMessage>();
        messageMock.Author.Returns(Substitute.For<IUser>());

        _discordSocketClient.CurrentUser.Returns(socketSelfUser);

        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.MessageReceived += Raise.Event<Func<IMessage, Task>>(messageMock);

        Assert.Empty(_messageDispatcher.ReceivedCalls());
    }

    [Fact]
    public async Task DispatchedMessageHasCorrectData()
    {
        DiscordMessage? actualMessage = null;
        _messageDispatcher.PublishAsync(Arg.Do<DiscordMessage>(x => actualMessage = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var socketSelfUser = Substitute.For<ISelfUser>();
        socketSelfUser.Id.Returns(5ul);

        _discordSocketClient.CurrentUser.Returns(socketSelfUser);

        const ulong channelId = 12345;
        const string channelName = "someChannel";
        var channelMock = Substitute.For<IMessageChannel>();
        channelMock.Name.Returns(channelName);
        channelMock.Id.Returns(channelId);

        const string username = "calledude";
        var userMock = Substitute.For<IGuildUser>();
        userMock.Username.Returns(username);

        const string content = "Hello! :D";
        var messageMock = Substitute.For<IUserMessage>();
        messageMock.Content.Returns(content);
        messageMock.Author.Returns(userMock);
        messageMock.Channel.Returns(channelMock);

        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.MessageReceived += Raise.Event<Func<IMessage, Task>>(messageMock);

        Assert.Equal(content, actualMessage!.Content);
        Assert.Equal($"#{channelName}", actualMessage!.Channel);
        Assert.Equal(username, actualMessage!.Sender!.Name);
        Assert.Equal(channelId, actualMessage!.Destination);

        await _messageDispatcher.Received(1).PublishAsync(Arg.Any<DiscordMessage>(), Arg.Any<CancellationToken>());
        Assert.Single(_messageDispatcher.ReceivedCalls());
    }

    [Fact]
    public async Task SendMessage_CallsCorrectMethods_WithCorrectArguments()
    {
        const string content = "hi! :)";
        const ulong channelId = 12345;

        var message = MessageObjectMother.CreateDiscordMessage(content, channelId);

        var messageChannelMock = Substitute.For<IMessageChannel>();
        _discordSocketClient.GetMessageChannel(Arg.Any<ulong>())
            .Returns(messageChannelMock);

        await _discordBot.SendMessageAsync(message);

        _discordSocketClient.Received(1).GetMessageChannel(channelId);
        await messageChannelMock
            .Received(1)
            .SendMessageAsync
            (
                content,
                Arg.Any<bool>(),
                Arg.Any<Embed>(),
                Arg.Any<RequestOptions>(),
                Arg.Any<AllowedMentions>(),
                Arg.Any<MessageReference>(),
                Arg.Any<MessageComponent>(),
                Arg.Any<ISticker[]>(),
                Arg.Any<Embed[]>(),
                Arg.Any<MessageFlags>()
            );
    }

    [Fact]
    public async Task Stop_ShutsClientDown()
    {
        await _discordBot.StopAsync(CancellationToken.None);

        await _discordSocketClient.Received(1).Logout();
        await _discordSocketClient.Received(1).Stop();
    }

    [Fact]
    public async Task Ready_PublishesReadyNotification()
    {
        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.Ready += Raise.Event<Func<Task>>();

        await _messageDispatcher.Received(1).PublishAsync(Arg.Any<ReadyNotification>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task GuildPermission_BanAndKick_CountsAsMod(bool banMembers, bool kickMembers)
    {
        DiscordMessage? actualMessage = null;
        _messageDispatcher.PublishAsync(Arg.Do<DiscordMessage>(x => actualMessage = x))
            .Returns(Task.CompletedTask);

        var socketSelfUser = Substitute.For<ISelfUser>();
        socketSelfUser.Id.Returns(5ul);

        _discordSocketClient.CurrentUser.Returns(socketSelfUser);

        var channelMock = Substitute.For<IMessageChannel>();

        var guildPermissions = new GuildPermissions(kickMembers: kickMembers, banMembers: banMembers);
        var userMock = Substitute.For<IGuildUser>();
        userMock.GuildPermissions.Returns(guildPermissions);

        var messageMock = Substitute.For<IUserMessage>();
        messageMock.Author.Returns(userMock);
        messageMock.Channel.Returns(channelMock);

        await _discordBot.StartAsync(CancellationToken.None);

        _discordSocketClient.MessageReceived += Raise.Event<Func<IMessage, Task>>(messageMock);

        var isMod = await actualMessage!.Sender!.IsModerator();

        Assert.True(isMod);
    }

    // TODO: Waiting on https://github.com/nsubstitute/NSubstitute/pull/715 to be merged

    //[Theory]
    //[InlineData(LogSeverity.Critical, null)]
    //[InlineData(LogSeverity.Critical, "")]
    //[InlineData(LogSeverity.Debug, "")]
    //[InlineData(LogSeverity.Warning, "")]
    //[InlineData(LogSeverity.Error, null)]
    //[InlineData(LogSeverity.Error, "")]
    //[InlineData(LogSeverity.Info, "")]
    //[InlineData(LogSeverity.Verbose, "")]
    //public async Task Log_CallsCorrectMethod(LogSeverity severity, string message)
    //{
    //    await _discordBot.StartAsync(CancellationToken.None);

    //    _discordSocketClient.Log += Raise.Event<Func<LogMessage, Task>>(new LogMessage(severity, string.Empty, message));

    //    void VerifyLogCall(LogLevel logLevel)
    //    {
    //        _logger
    //            .Received(1)
    //            .Log
    //            (
    //                logLevel,
    //                Arg.Any<EventId>(),
    //                Arg.Any<Arg.AnyType>(),
    //                Arg.Any<Exception?>(),

    //                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
    //            );
    //    }

    //    switch (severity)
    //    {
    //        case LogSeverity.Critical:
    //            VerifyLogCall(LogLevel.Critical);
    //            break;
    //        case LogSeverity.Error:
    //            VerifyLogCall(LogLevel.Error);
    //            break;
    //        case LogSeverity.Warning:
    //            VerifyLogCall(LogLevel.Warning);
    //            break;
    //        case LogSeverity.Info:
    //            VerifyLogCall(LogLevel.Information);
    //            break;
    //        case LogSeverity.Verbose:
    //            VerifyLogCall(LogLevel.Trace);
    //            break;
    //        case LogSeverity.Debug:
    //            VerifyLogCall(LogLevel.Debug);
    //            break;
    //    }
    //}
}