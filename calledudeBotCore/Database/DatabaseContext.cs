using calledudeBot.Database.Activity;
using calledudeBot.Database.Compiled;
using calledudeBot.Database.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace calledudeBot.Database;

public class DatabaseContext : DbContext
{
	private readonly ILoggerFactory? _loggerFactory;

	public DatabaseContext() { }

	public DatabaseContext(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
	}

	public virtual DbSet<UserActivity> UserActivities => Set<UserActivity>();
	public virtual DbSet<UserSession> UserSession => Set<UserSession>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseSqlite("Data Source=calledudeBot.db")
						.UseLoggerFactory(_loggerFactory)
						.UseModel(DatabaseContextModel.Instance);
}