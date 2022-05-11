using calledudeBot.Database;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Database;

public class UserSessionRepositoryTests
{
	private readonly Mock<DatabaseContext> _dbContext;
	private readonly UserSessionRepository _userSessionRepository;

	public UserSessionRepositoryTests()
	{
		_dbContext = new Mock<DatabaseContext>();
		_userSessionRepository = new UserSessionRepository(_dbContext.Object);
	}

	[Fact]
	public async Task UserSessionIsTrackedProperly()
	{
		UserSessionEntity? actualEntity = null;
		var dbSet = new Mock<DbSet<UserSessionEntity>>();
		dbSet
			.Setup(x => x.AddAsync(It.IsAny<UserSessionEntity>(), It.IsAny<CancellationToken>()))
			.Callback((UserSessionEntity entity, CancellationToken _) => actualEntity = entity);

		_dbContext.Setup(x => x.UserSession).ReturnsDbSet(Enumerable.Empty<UserSessionEntity>(), dbSet);

		var now = DateTime.Now;
		var userActivityEntity = new UserActivityEntity
		{
			Username = "calledude",
			LastJoinDate = now.AddMinutes(-5)
		};

		await _userSessionRepository.TrackUserSession(userActivityEntity);

		dbSet.Verify(x => x.AddAsync(It.IsAny<UserSessionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
		_dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		Assert.NotNull(actualEntity);
		Assert.Equal(userActivityEntity.Username, actualEntity!.Username);
		Assert.Equal(userActivityEntity.LastJoinDate, actualEntity.StartTime);
		Assert.InRange(actualEntity.EndTime, now.AddSeconds(-1), now.AddSeconds(1));
		Assert.Equal(actualEntity.EndTime - userActivityEntity.LastJoinDate, actualEntity.WatchTime);
	}

	[Fact]
	public async Task GetUserSession_ReturnsCorrectUser()
	{
		const string userName = "calledude";
		var userSession1 = new UserSessionEntity
		{
			Username = userName,
		};
		var userSession2 = new UserSessionEntity
		{
			Username = userName,
		};

		var userSession3 = new UserSessionEntity
		{
			Username = "someOtherUser"
		};

		var userActivities = new List<UserSessionEntity>
		{
			userSession1,
			userSession2,
			userSession3
		};

		var dbSet = new Mock<DbSet<UserSessionEntity>>();
		_dbContext.Setup(x => x.UserSession).ReturnsDbSet(userActivities, dbSet);

		var actualUserSessions = await _userSessionRepository.GetUserSessions(userName);

		Assert.Equal(2, actualUserSessions.Count);
		Assert.Contains(userSession1, actualUserSessions);
		Assert.Contains(userSession2, actualUserSessions);
	}
}
