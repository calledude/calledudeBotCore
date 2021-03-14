using calledudeBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Database.UserActivity
{
    public interface IUserActivityRepository
    {
        Task<UserActivityEntity?> GetUserActivity(string user);
        Task SaveUserActivity(UserParticipationNotification notification);
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
            var entity = await _context
                .UserActivities
                .AsTracking()
                .Where(x => x.Username == notification.User.Name)
                .FirstOrDefaultAsync();

            if (entity == default)
            {
                entity = new UserActivityEntity
                {
                    LastJoinDate = notification.When,
                    Username = notification.User.Name,
                    TimesSeen = 1
                };

                await _context.UserActivities!.AddAsync(entity);
            }
            else
            {
                entity.TimesSeen++;
                _context.UserActivities!.Update(entity);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserActivityEntity?> GetUserActivity(string user)
        {
            return await _context.UserActivities
                .AsNoTracking()
                .Where(x => x.Username == user)
                .FirstOrDefaultAsync();
        }
    }
}