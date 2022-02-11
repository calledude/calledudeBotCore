using calledudeBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Database.UserActivity;

public interface IUserActivityRepository
{
    Task<UserActivityEntity?> GetUserActivity(string user);
    Task SaveUserActivity(UserParticipationNotification notification);
    Task SaveUserChatActivity(string userName);
}

public class UserActivityRepository : IUserActivityRepository
{
    private readonly DatabaseContext _context;

    public UserActivityRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task SaveUserActivity(UserParticipationNotification notification)
    {
        var entity = await GetTrackedUserActivity(notification.User.Name);

        if (entity == default)
        {
            entity = new UserActivityEntity
            {
                LastJoinDate = notification.When,
                Username = notification.User.Name,
                TimesSeen = 1
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

    private async Task<UserActivityEntity?> GetTrackedUserActivity(string userName)
        => await _context
                .UserActivities
                .AsTracking()
                .Where(x => x.Username == userName)
                .FirstOrDefaultAsync();

    public async Task SaveUserChatActivity(string userName)
    {
        var entity = await GetTrackedUserActivity(userName);

        if (entity is null)
            return;

        entity.MessagesSent++;

        await _context.SaveChangesAsync();
    }

    public async Task<UserActivityEntity?> GetUserActivity(string user)
        => await _context.UserActivities
            .AsNoTracking()
            .Where(x => x.Username == user)
            .FirstOrDefaultAsync();
}
