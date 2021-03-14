using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace calledudeBot.Database
{
    public class DatabaseContext : DbContext
    {
        private readonly ILoggerFactory? _loggerFactory;

        public DatabaseContext() { }

        public DatabaseContext(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public DbSet<UserActivityEntity>? UserActivities { get; set; }
        public DbSet<UserSessionEntity>? UserSession { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=calledudeBot.db")
                            .UseLoggerFactory(_loggerFactory);
    }
}
