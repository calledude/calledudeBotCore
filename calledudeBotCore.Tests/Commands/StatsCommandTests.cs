using calledudeBot.Chat.Commands;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
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

    private readonly List<UserSessionEntity> _userSessionsSingle;
    private readonly UserActivityEntity _userActivity;
    private readonly List<UserSessionEntity> _userSessionsMultiple;

    private readonly UserSessionEntity _userSession1;

    private readonly UserSessionEntity _userSession2;

    public StatsCommandTests()
    {
        _userSessionService = new Mock<IUserSessionService>();
        _userActivityService = new Mock<IUserActivityService>();
        _userSession1 = new UserSessionEntity
        {
            WatchTime = TimeSpan.FromSeconds(4)
        };

        _userSession2 = new UserSessionEntity
        {
            WatchTime = TimeSpan.FromSeconds(19)
        };

        _userSessionsSingle = new List<UserSessionEntity>
        {
            _userSession1
        };

        _userSessionsMultiple = new List<UserSessionEntity>
        {
            _userSession1,
            _userSession2
        };

        _userActivity = new UserActivityEntity
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
