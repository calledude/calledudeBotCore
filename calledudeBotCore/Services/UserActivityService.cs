using calledudeBot.Chat;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using calledudeBot.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IUserActivityService : INotificationHandler<UserParticipationNotification>, INotificationHandler<IMessage<IrcMessage>>
{
    Task<UserActivityEntity?> GetUserActivity(string userName);
}

public class UserActivityService : IUserActivityService
{
    private readonly ILogger<UserActivityService> _logger;
    private readonly IUserActivityRepository _userActivityRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IStreamMonitor _streamMonitor;

    public UserActivityService(ILogger<UserActivityService> logger,
                               IUserActivityRepository userActivityRepository,
                               IUserSessionRepository userSessionRepository,
                               IStreamMonitor streamMonitor)
    {
        _logger = logger;
        _userActivityRepository = userActivityRepository;
        _userSessionRepository = userSessionRepository;
        _streamMonitor = streamMonitor;
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

    private async Task HandleJoin(UserParticipationNotification notification)
        => await _userActivityRepository.SaveUserActivity(notification);

    public async Task<UserActivityEntity?> GetUserActivity(string userName)
        => await _userActivityRepository.GetUserActivity(userName);

    private async Task HandleLeave(UserParticipationNotification notification)
    {
        var user = await GetUserActivity(notification.User.Name);

        if (user is null)
        {
            _logger.LogWarning("Tried tracking session for user: '{userName}' but no start point was available.", notification.User.Name);
            return;
        }

        if (_streamMonitor.IsStreaming && _streamMonitor.StreamStarted < DateTime.Now)
        {
            _logger.LogWarning("Tried tracking session for user: '{userName}' but the startpoint: {lastJoinDate} was invalid.", notification.User.Name, user.LastJoinDate);
            return;
        }

        _logger.LogInformation("Logged user session for '{userName}'", notification.User.Name);
        await _userSessionRepository.TrackUserSession(user);
    }

    public async Task Handle(IMessage<IrcMessage> notification, CancellationToken cancellationToken)
        => await _userActivityRepository.SaveUserChatActivity(notification.Sender.Name);
}
