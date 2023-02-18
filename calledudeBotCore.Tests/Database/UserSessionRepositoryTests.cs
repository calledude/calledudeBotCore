using calledudeBot.Database;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
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
		UserSession? actualEntity = null;
		var dbSet = new Mock<DbSet<UserSession>>();
		dbSet
			.Setup(x => x.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
			.Callback((UserSession entity, CancellationToken _) => actualEntity = entity);

		_dbContext.Setup(x => x.UserSession).ReturnsDbSet(Enumerable.Empty<UserSession>(), dbSet);

		var now = DateTime.Now;
		var userActivityEntity = new UserActivity
		{
			Username = "calledude",
			LastJoinDate = now.AddMinutes(-5)
		};

		await _userSessionRepository.TrackUserSession(userActivityEntity);

		dbSet.Verify(x => x.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Once);
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
		var userSession1 = new UserSession
		{
			Username = userName,
		};
		var userSession2 = new UserSession
		{
			Username = userName,
		};

		var userSession3 = new UserSession
		{
			Username = "someOtherUser"
		};

		var userActivities = new List<UserSession>
		{
			userSession1,
			userSession2,
			userSession3
		};

		var dbSet = new Mock<DbSet<UserSession>>();
		_dbContext.Setup(x => x.UserSession).ReturnsDbSet(userActivities, dbSet);

		var actualUserSessions = await _userSessionRepository.GetUserSessions(userName);

		Assert.Equal(2, actualUserSessions.Count);
		Assert.Contains(userSession1, actualUserSessions);
		Assert.Contains(userSession2, actualUserSessions);
	}
}
