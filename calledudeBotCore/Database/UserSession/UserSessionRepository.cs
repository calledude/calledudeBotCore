using calledudeBot.Database.UserActivity;
using System;
using System.Threading.Tasks;

namespace calledudeBot.Database.UserSession
{
    public interface IUserSessionRepository
    {
        Task TrackUserSession(UserActivityEntity entity);
    }

    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly DatabaseContext _context;

        public UserSessionRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task TrackUserSession(UserActivityEntity activityEntity)
        {
            var endTime = DateTime.Now;

            var session = new UserSessionEntity
            {
                Username = activityEntity.Username,
                StartTime = activityEntity.LastJoinDate,
                EndTime = endTime,
                WatchTime = endTime - activityEntity.LastJoinDate
            };

            await _context.UserSession!.AddAsync(session);
        }
    }
}
