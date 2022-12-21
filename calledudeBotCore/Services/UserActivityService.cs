using calledudeBot.Chat;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IUserActivityService : INotificationHandler<UserParticipationNotification>, INotificationHandler<IrcMessage>
{
	Task<UserActivityEntity?> GetUserActivity(string? userName);
}

public class UserActivityService : IUserActivityService
{
	private readonly ILogger<UserActivityService> _logger;
	private readonly IUserActivityRepository _userActivityRepository;
	private readonly IUserSessionRepository _userSessionRepository;
	private readonly IStreamingState _streamingState;

	public UserActivityService(
		ILogger<UserActivityService> logger,
		IUserActivityRepository userActivityRepository,
		IUserSessionRepository userSessionRepository,
		IStreamingState streamingState)
	{
		_logger = logger;
		_userActivityRepository = userActivityRepository;
		_userSessionRepository = userSessionRepository;
		_streamingState = streamingState;
	}

	public async Task Handle(UserParticipationNotification notification, CancellationToken cancellationToken)
	{
		if (notification.ParticipationType == ParticipationType.Join)
		{
			await HandleJoin(notification);
		}
		else if (notification.ParticipationType == ParticipationType.Leave)
		{
			await HandleLeave(notification);
		}
	}

	public async Task Handle(IrcMessage notification, CancellationToken cancellationToken)
		=> await _userActivityRepository.SaveUserChatActivity(notification.Sender?.Name);

	private async Task HandleJoin(UserParticipationNotification notification)
		=> await _userActivityRepository.SaveUserActivity(notification, _streamingState.SessionId);

	public async Task<UserActivityEntity?> GetUserActivity(string? userName)
		=> await _userActivityRepository.GetUserActivity(userName);

	private async Task HandleLeave(UserParticipationNotification notification)
	{
		_logger.LogTrace("Trying to track session for user '{userName}' in streaming session '{streamSession}'", notification.User.Name, _streamingState.SessionId);

		var user = await GetUserActivity(notification.User.Name);
		if (user is null || (user.StreamSession != _streamingState.SessionId && user.LastJoinDate < _streamingState.StreamStarted))
		{
			_logger.LogWarning("Tried tracking session for user: '{userName}' but no start point was available.", notification.User.Name);
			return;
		}

		// If we don't have a SessionId for the UserActivity but the LastJoinDate is within the stream uptime span, we count that as a valid session
		_logger.LogInformation("Logged user session for '{userName}'", notification.User.Name);
		await _userSessionRepository.TrackUserSession(user);
	}
}
