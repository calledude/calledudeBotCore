using calledudeBot.Database.UserActivity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Database.UserSession;

public interface IUserSessionRepository
{
    Task TrackUserSession(UserActivityEntity entity);
    Task<List<UserSessionEntity>> GetUserSessions(string user);
}

public class UserSessionRepository : IUserSessionRepository
{
    private readonly DatabaseContext _context;

    public UserSessionRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task TrackUserSession(UserActivityEntity entity)
    {
        var endTime = DateTime.Now;

        var session = new UserSessionEntity
        {
            Username = entity.Username,
            StartTime = entity.LastJoinDate,
            EndTime = endTime,
            WatchTime = endTime - entity.LastJoinDate
        };

        await _context.UserSession.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserSessionEntity>> GetUserSessions(string user)
        => await _context
                   .UserSession
                   .Where(x => x.Username == user)
                   .ToListAsync();
}
