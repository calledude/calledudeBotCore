using calledudeBot.Chat.Commands;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class StatsCommandTests
{
    private readonly IUserSessionService _userSessionService;
    private readonly IUserActivityService _userActivityService;

    private readonly List<UserSession> _userSessionsSingle;
    private readonly UserActivity _userActivity;
    private readonly List<UserSession> _userSessionsMultiple;

    private readonly UserSession _userSession1;

    private readonly UserSession _userSession2;

    public StatsCommandTests()
    {
        _userSessionService = Substitute.For<IUserSessionService>();
        _userActivityService = Substitute.For<IUserActivityService>();
        _userSession1 = new UserSession
        {
            WatchTime = TimeSpan.FromSeconds(4)
        };

        _userSession2 = new UserSession
        {
            WatchTime = TimeSpan.FromSeconds(19)
        };

        _userSessionsSingle =
        [
            _userSession1
        ];

        _userSessionsMultiple =
        [
            _userSession1,
            _userSession2
        ];

        _userActivity = new UserActivity
        {
            TimesSeen = 5,
            MessagesSent = 3
        };
    }

    [Fact]
    public async Task UserNotFound()
    {
        const string username = "someUser";

        var commandParameter = CommandParameterObjectMother.CreateWithEmptyMessageAndUserName(username);

        var target = new StatsCommand(_userSessionService, _userActivityService);
        var result = await target.Handle(commandParameter);

        await _userActivityService.Received(1).GetUserActivity(Arg.Is<string>(y => y == username));
        Assert.Single(_userActivityService.ReceivedCalls());
        Assert.Empty(_userSessionService.ReceivedCalls());

        Assert.Equal($"User {username} has no recorded activity.", result);
    }

    [Fact]
    public async Task MessageContentContainsUser_FetchesUserFromContent()
    {
        const string username = "someUserFromMessageContent";

        var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(username);

        _userActivityService
            .GetUserActivity(Arg.Any<string>())
            .Returns(_userActivity);

        _userSessionService
            .GetUserSessions(Arg.Any<string>())
            .Returns(_userSessionsSingle);

        var target = new StatsCommand(_userSessionService, _userActivityService);
        var result = await target.Handle(commandParameter);

        await _userActivityService.Received(1).GetUserActivity(Arg.Is<string>(y => y == username));
        Assert.Single(_userActivityService.ReceivedCalls());
        await _userSessionService.Received(1).GetUserSessions(Arg.Is<string>(y => y == username));
        Assert.Single(_userSessionService.ReceivedCalls());

        Assert.Equal($"User {username} | Total watchtime: {_userSessionsSingle[0].WatchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }

    [Fact]
    public async Task MessageContentContainsNoUser_FetchesUserFromMessageSender()
    {
        const string username = "someUser";

        var commandParameter = CommandParameterObjectMother.CreateWithEmptyMessageAndUserName(username);

        _userActivityService
            .GetUserActivity(Arg.Any<string>())
            .Returns(_userActivity);

        _userSessionService
            .GetUserSessions(Arg.Any<string>())
            .Returns(_userSessionsSingle);

        var target = new StatsCommand(_userSessionService, _userActivityService);
        var result = await target.Handle(commandParameter);

        await _userActivityService.Received(1).GetUserActivity(Arg.Is<string>(y => y == username));
        Assert.Single(_userActivityService.ReceivedCalls());
        await _userSessionService.Received(1).GetUserSessions(Arg.Is<string>(y => y == username));
        Assert.Single(_userSessionService.ReceivedCalls());

        Assert.Equal($"User {username} | Total watchtime: {_userSessionsSingle[0].WatchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }

    [Fact]
    public async Task WatchTimeIsAggregatedCorrectly()
    {
        const string username = "someUser";

        var commandParameter = CommandParameterObjectMother.CreateWithEmptyMessageAndUserName(username);

        _userActivityService
            .GetUserActivity(Arg.Any<string>())
            .Returns(_userActivity);

        _userSessionService
            .GetUserSessions(Arg.Any<string>())
            .Returns(_userSessionsMultiple);

        var target = new StatsCommand(_userSessionService, _userActivityService);
        var result = await target.Handle(commandParameter);

        await _userActivityService.Received(1).GetUserActivity(Arg.Is<string>(y => y == username));
        Assert.Single(_userActivityService.ReceivedCalls());
        await _userSessionService.Received(1).GetUserSessions(Arg.Is<string>(y => y == username));
        Assert.Single(_userSessionService.ReceivedCalls());

        var watchTime = _userSession1.WatchTime + _userSession2.WatchTime;

        Assert.Equal($"User {username} | Total watchtime: {watchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }
}
