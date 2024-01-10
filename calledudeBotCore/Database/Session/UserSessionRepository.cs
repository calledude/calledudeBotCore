using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Database.Session;

public interface IUserSessionRepository
{
	Task TrackUserSession(Activity.UserActivity entity);
	Task<List<UserSession>> GetUserSessions(string? user);
}

public class UserSessionRepository : IUserSessionRepository
{
	private readonly DatabaseContext _context;

	public UserSessionRepository(DatabaseContext context)
	{
		_context = context;
	}

	public async Task TrackUserSession(Activity.UserActivity entity)
	{
		var endTime = DateTime.UtcNow;

		var session = new UserSession
		{
			Username = entity.Username,
			StartTime = entity.LastJoinDate,
			EndTime = endTime,
			WatchTime = endTime - entity.LastJoinDate
		};

		await _context.UserSession.AddAsync(session);
		await _context.SaveChangesAsync();
	}

	public async Task<List<UserSession>> GetUserSessions(string? user)
		=> await _context
					.UserSession
					.Where(x => x.Username == user)
					.ToListAsync();
}
