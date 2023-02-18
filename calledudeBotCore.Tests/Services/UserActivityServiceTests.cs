using calledudeBot.Chat;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class UserActivityServiceTests
{
	private readonly UserActivityService _userActivityService;
	private readonly Mock<IStreamingState> _streamingState;
	private readonly Mock<IUserActivityRepository> _userActivityRepository;
	private readonly Mock<IUserSessionRepository> _userSessionRepository;
	private static readonly Logger<UserActivityService> _logger = LoggerObjectMother.NullLoggerFor<UserActivityService>();

	public UserActivityServiceTests()
	{
		_streamingState = new Mock<IStreamingState>();
		_userActivityRepository = new Mock<IUserActivityRepository>();
		_userSessionRepository = new Mock<IUserSessionRepository>();
		_userActivityService = new UserActivityService(_logger, _userActivityRepository.Object, _userSessionRepository.Object, _streamingState.Object);
	}

	[Fact]
	public async Task ParticipationType_Join_CallsSaveUserActivity()
	{
		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Join);
		var sessionId = Guid.NewGuid();

		_streamingState
			.SetupGet(x => x.SessionId)
			.Returns(sessionId);

		await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

		_streamingState.VerifyGet(x => x.SessionId, Times.Once);
		_streamingState.VerifyNoOtherCalls();
		_userSessionRepository.VerifyNoOtherCalls();

		_userActivityRepository.Verify(x => x.SaveUserActivity(userParticipationNotification, sessionId), Times.Once);
		_userActivityRepository.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ParticipationType_Leave_NullUser_Bails()
	{
		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

		await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

		_streamingState.VerifyGet(x => x.SessionId, Times.Once);
		_streamingState.VerifyNoOtherCalls();

		_userActivityRepository.Verify(x => x.GetUserActivity(It.IsAny<string>()), Times.Once);
		_userSessionRepository.VerifyNoOtherCalls();
		_userActivityRepository.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ParticipationType_Leave_Invalid_TrackingPoint()
	{
		var now = DateTime.Now;

		_userActivityRepository
			.Setup(x => x.GetUserActivity(It.IsAny<string>()))
			.ReturnsAsync(new UserActivity
			{
				LastJoinDate = now,
				StreamSession = Guid.Empty
			});

		_streamingState.SetupGet(x => x.StreamStarted).Returns(now.AddMinutes(5));
		_streamingState.SetupGet(x => x.SessionId).Returns(Guid.NewGuid());

		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

		await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

		_streamingState.VerifyGet(x => x.SessionId, Times.Exactly(2));
		_streamingState.VerifyGet(x => x.StreamStarted, Times.Once);
		_streamingState.VerifyNoOtherCalls();

		_userSessionRepository.VerifyNoOtherCalls();

		_userActivityRepository.Verify(x => x.GetUserActivity(It.IsAny<string>()), Times.Once);
		_userActivityRepository.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ParticipationType_Leave_Valid_SessionIsTracked()
	{
		var now = DateTime.Now;
		var streamSession = Guid.NewGuid();

		_userActivityRepository
			.Setup(x => x.GetUserActivity(It.IsAny<string>()))
			.ReturnsAsync(new UserActivity
			{
				LastJoinDate = now,
				StreamSession = streamSession
			});

		_streamingState.SetupGet(x => x.StreamStarted).Returns(now.AddMinutes(-5));
		_streamingState.SetupGet(x => x.SessionId).Returns(streamSession);

		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Empty, ParticipationType.Leave);

		await _userActivityService.Handle(userParticipationNotification, CancellationToken.None);

		_streamingState.VerifyGet(x => x.SessionId, Times.Exactly(2));
		_streamingState.VerifyNoOtherCalls();

		_userSessionRepository.Verify(x => x.TrackUserSession(It.IsAny<UserActivity>()), Times.Once);
		_userSessionRepository.VerifyNoOtherCalls();

		_userActivityRepository.Verify(x => x.GetUserActivity(It.IsAny<string>()), Times.Once);
		_userActivityRepository.VerifyNoOtherCalls();
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

		_userActivityRepository.Verify(x => x.SaveUserChatActivity(userName), Times.Once);
		_userActivityRepository.VerifyNoOtherCalls();
	}
}
