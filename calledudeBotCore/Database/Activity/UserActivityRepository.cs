using calledudeBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Database.Activity;

public interface IUserActivityRepository
{
	Task<UserActivity?> GetUserActivity(string? user);
	Task SaveUserActivity(UserParticipationNotification notification, Guid streamSession);
	Task SaveUserChatActivity(string? userName);
}

public class UserActivityRepository : IUserActivityRepository
{
	private readonly DatabaseContext _context;

	public UserActivityRepository(DatabaseContext context)
	{
		_context = context;
	}

	public async Task SaveUserActivity(UserParticipationNotification notification, Guid streamSession)
	{
		var entity = await GetTrackedUserActivity(notification.User.Name);

		if (entity == default)
		{
			entity = new UserActivity
			{
				LastJoinDate = notification.When,
				Username = notification.User.Name,
				TimesSeen = 1,
				StreamSession = streamSession
			};

			await _context.UserActivities.AddAsync(entity);
		}
		else
		{
			entity.TimesSeen++;
			_context.UserActivities.Update(entity);
		}

		await _context.SaveChangesAsync();
	}

	private async Task<UserActivity?> GetTrackedUserActivity(string? userName)
		=> await _context.UserActivities
				.AsTracking()
				.Where(x => x.Username == userName)
				.FirstOrDefaultAsync();

	public async Task SaveUserChatActivity(string? userName)
	{
		var entity = await GetTrackedUserActivity(userName);

		if (entity is null)
			return;

		entity.MessagesSent++;

		await _context.SaveChangesAsync();
	}

	public async Task<UserActivity?> GetUserActivity(string? user)
		=> await _context.UserActivities
			.AsNoTracking()
			.Where(x => x.Username == user)
			.FirstOrDefaultAsync();
}
