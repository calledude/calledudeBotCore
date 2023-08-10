using calledudeBot.Chat;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
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

namespace calledudeBotCore.Tests.Services;

public class UserActivityServiceTests
{
    private readonly UserActivityService _userActivityService;
    private readonly IStreamingState _streamingState;
    private readonly IUserActivityRepository _userActivityRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private static readonly Logger<UserActivityService> _logger = LoggerObjectMother.NullLoggerFor<UserActivityService>();

    public UserActivityServiceTests()
    {
        _streamingState = Substitute.For<IStreamingState>();
        _userActivityRepository = Substitute.For<IUserActivityRepository>();
        _userSessionRepository = Substitute.For<IUserSessionRepository>();
        _userActivityService = new UserActivityService(_logger, _userActivityRepository, _userSessionRepository, _streamingState);
    }

    [Fact]
    public async Task ParticipationType_Join_CallsSaveUserActivity()
    {
        var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Join);
        var sessionId = Guid.NewGuid();

        _streamingState.SessionId.Returns(sessionId);

        await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

        _ = _streamingState.Received(1).SessionId;
        Assert.Single(_streamingState.ReceivedCalls());
        Assert.Empty(_userSessionRepository.ReceivedCalls());

        await _userActivityRepository.Received(1).SaveUserActivity(userParticipationNotification, sessionId);
        Assert.Single(_userActivityRepository.ReceivedCalls());
    }

    [Fact]
    public async Task ParticipationType_Leave_NullUser_Bails()
    {
        var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

        await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

        _ = _streamingState.Received(1).SessionId;
        Assert.Single(_streamingState.ReceivedCalls());
        Assert.Empty(_userSessionRepository.ReceivedCalls());

        await _userActivityRepository.Received(1).GetUserActivity(Arg.Any<string>());
        Assert.Single(_userActivityRepository.ReceivedCalls());
    }

    [Fact]
    public async Task ParticipationType_Leave_Invalid_TrackingPoint()
    {
        var now = DateTime.Now;

        _userActivityRepository.GetUserActivity(Arg.Any<string>())
            .Returns(new UserActivity
            {
                LastJoinDate = now,
                StreamSession = Guid.Empty
            });

        _streamingState.StreamStarted.Returns(now.AddMinutes(5));
        _streamingState.SessionId.Returns(Guid.NewGuid());

        var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

        await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

        _ = _streamingState.Received(2).SessionId;
        _ = _streamingState.Received(1).StreamStarted;
        Assert.Equal(3, _streamingState.ReceivedCalls().Count());

        Assert.Empty(_userSessionRepository.ReceivedCalls());

        await _userActivityRepository.Received(1).GetUserActivity(Arg.Any<string>());
        Assert.Single(_userActivityRepository.ReceivedCalls());
    }

    [Fact]
    public async Task ParticipationType_Leave_Valid_SessionIsTracked()
    {
        var now = DateTime.Now;
        var streamSession = Guid.NewGuid();

        _userActivityRepository.GetUserActivity(Arg.Any<string>())
            .Returns(new UserActivity
            {
                LastJoinDate = now,
                StreamSession = streamSession
            });

        _streamingState.StreamStarted.Returns(now.AddMinutes(-5));
        _streamingState.SessionId.Returns(streamSession);

        var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

        await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

        _ = _streamingState.Received(2).SessionId;
        Assert.Equal(2, _streamingState.ReceivedCalls().Count());

        await _userSessionRepository.Received(1).TrackUserSession(Arg.Any<UserActivity>());
        Assert.Single(_userSessionRepository.ReceivedCalls());

        await _userActivityRepository.Received(1).GetUserActivity(Arg.Any<string>());
        Assert.Single(_userSessionRepository.ReceivedCalls());
    }

    [Fact]
    public async Task UserChatActivity_IsSaved()
    {
        const string userName = "calledude";
        var notification = new IrcMessage()
        {
            Content = "",
            Sender = UserObjectMother.Create(userName)
        };

        await _userActivityService.Handle(notification, CancellationToken.None);

        await _userActivityRepository.Received(1).SaveUserChatActivity(userName);
        Assert.Single(_userActivityRepository.ReceivedCalls());
    }
}
