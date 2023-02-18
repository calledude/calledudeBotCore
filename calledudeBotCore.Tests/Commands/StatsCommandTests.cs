using calledudeBot.Chat.Commands;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class StatsCommandTests
{
    private readonly Mock<IUserSessionService> _userSessionService;
    private readonly Mock<IUserActivityService> _userActivityService;

    private readonly List<UserSession> _userSessionsSingle;
    private readonly UserActivity _userActivity;
    private readonly List<UserSession> _userSessionsMultiple;

    private readonly UserSession _userSession1;

    private readonly UserSession _userSession2;

    public StatsCommandTests()
    {
        _userSessionService = new Mock<IUserSessionService>();
        _userActivityService = new Mock<IUserActivityService>();
        _userSession1 = new UserSession
        {
            WatchTime = TimeSpan.FromSeconds(4)
        };

        _userSession2 = new UserSession
        {
            WatchTime = TimeSpan.FromSeconds(19)
        };

        _userSessionsSingle = new List<UserSession>
        {
            _userSession1
        };

        _userSessionsMultiple = new List<UserSession>
        {
            _userSession1,
            _userSession2
        };

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

        var target = new StatsCommand(_userSessionService.Object, _userActivityService.Object);
        var result = await target.Handle(commandParameter);

        _userActivityService.Verify(x => x.GetUserActivity(It.Is<string>(y => y == username)));
        _userActivityService.VerifyNoOtherCalls();
        _userSessionService.VerifyNoOtherCalls();

        Assert.Equal($"User {username} has no recorded activity.", result);
    }

    [Fact]
    public async Task MessageContentContainsUser_FetchesUserFromContent()
    {
        const string username = "someUserFromMessageContent";

        var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(username);

        _userActivityService
            .Setup(x => x.GetUserActivity(It.IsAny<string>()))
            .ReturnsAsync(_userActivity);

        _userSessionService
            .Setup(x => x.GetUserSessions(It.IsAny<string>()))
            .ReturnsAsync(_userSessionsSingle);

        var target = new StatsCommand(_userSessionService.Object, _userActivityService.Object);
        var result = await target.Handle(commandParameter);

        _userActivityService.Verify(x => x.GetUserActivity(It.Is<string>(y => y == username)));
        _userActivityService.VerifyNoOtherCalls();
        _userSessionService.Verify(x => x.GetUserSessions(It.Is<string>(y => y == username)));
        _userSessionService.VerifyNoOtherCalls();

        Assert.Equal($"User {username} | Total watchtime: {_userSessionsSingle[0].WatchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }

    [Fact]
    public async Task MessageContentContainsNoUser_FetchesUserFromMessageSender()
    {
        const string username = "someUser";

        var commandParameter = CommandParameterObjectMother.CreateWithEmptyMessageAndUserName(username);

        _userActivityService
            .Setup(x => x.GetUserActivity(It.IsAny<string>()))
            .ReturnsAsync(_userActivity);

        _userSessionService
            .Setup(x => x.GetUserSessions(It.IsAny<string>()))
            .ReturnsAsync(_userSessionsSingle);

        var target = new StatsCommand(_userSessionService.Object, _userActivityService.Object);
        var result = await target.Handle(commandParameter);

        _userActivityService.Verify(x => x.GetUserActivity(It.Is<string>(y => y == username)));
        _userActivityService.VerifyNoOtherCalls();
        _userSessionService.Verify(x => x.GetUserSessions(It.Is<string>(y => y == username)));
        _userSessionService.VerifyNoOtherCalls();

        Assert.Equal($"User {username} | Total watchtime: {_userSessionsSingle[0].WatchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }

    [Fact]
    public async Task WatchTimeIsAggregatedCorrectly()
    {
        const string username = "someUser";

        var commandParameter = CommandParameterObjectMother.CreateWithEmptyMessageAndUserName(username);

        _userActivityService
            .Setup(x => x.GetUserActivity(It.IsAny<string>()))
            .ReturnsAsync(_userActivity);

        _userSessionService
            .Setup(x => x.GetUserSessions(It.IsAny<string>()))
            .ReturnsAsync(_userSessionsMultiple);

        var target = new StatsCommand(_userSessionService.Object, _userActivityService.Object);
        var result = await target.Handle(commandParameter);

        _userActivityService.Verify(x => x.GetUserActivity(It.Is<string>(y => y == username)));
        _userActivityService.VerifyNoOtherCalls();
        _userSessionService.Verify(x => x.GetUserSessions(It.Is<string>(y => y == username)));
        _userSessionService.VerifyNoOtherCalls();

        var watchTime = _userSession1.WatchTime + _userSession2.WatchTime;

        Assert.Equal($"User {username} | Total watchtime: {watchTime} | Seen: {_userActivity.TimesSeen} times | Messages: {_userActivity.MessagesSent}", result);
    }
}
