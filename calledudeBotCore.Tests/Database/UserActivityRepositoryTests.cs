using calledudeBot.Database;
using calledudeBot.Database.Activity;
using calledudeBot.Models;
using calledudeBotCore.Tests.ObjectMothers;
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

public class UserActivityRepositoryTests
{
	private readonly Mock<DatabaseContext> _dbContext;
	private readonly UserActivityRepository _userActivityRepository;

	public UserActivityRepositoryTests()
	{
		_dbContext = new Mock<DatabaseContext>();
		_userActivityRepository = new UserActivityRepository(_dbContext.Object);
	}

	[Fact]
	public async Task SavingNewUserActivity()
	{
		var userActivities = Enumerable.Empty<UserActivity>();

		UserActivity? actualEntity = null;
		var dbSet = new Mock<DbSet<UserActivity>>();
		dbSet
			.Setup(x => x.AddAsync(It.IsAny<UserActivity>(), It.IsAny<CancellationToken>()))
			.Callback((UserActivity entity, CancellationToken _) => actualEntity = entity);

		_dbContext.Setup(x => x.UserActivities).ReturnsDbSet(userActivities, dbSet);

		var now = DateTime.Now;
		const string userName = "calledude";
		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Create(userName), ParticipationType.Leave)
		{
			When = now
		};

		var streamSessionId = Guid.NewGuid();
		await _userActivityRepository.SaveUserActivity(userParticipationNotification, streamSessionId);

		Assert.NotNull(actualEntity);
		Assert.Equal(now, actualEntity!.LastJoinDate);
		Assert.Equal(userName, actualEntity.Username);
		Assert.Equal(1, actualEntity.TimesSeen);
		Assert.Equal(streamSessionId, actualEntity.StreamSession);

		_dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UpdatingExistingUserActivity()
	{
		var oldLastJoinDate = DateTime.Now;
		const string userName = "calledude";
		const int oldMessagesSent = 2;
		var oldStreamSessionId = Guid.NewGuid();
		const int oldTimesSeen = 4;
		var existingUserActivity = new UserActivity
		{
			LastJoinDate = oldLastJoinDate,
			Username = userName,
			MessagesSent = oldMessagesSent,
			StreamSession = oldStreamSessionId,
			TimesSeen = oldTimesSeen
		};

		var userActivities = new List<UserActivity>
		{
			existingUserActivity
		};

		var dbSet = new Mock<DbSet<UserActivity>>();
		_dbContext.Setup(x => x.UserActivities).ReturnsDbSet(userActivities, dbSet);

		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Create(userName), ParticipationType.Leave)
		{
			When = DateTime.Now.AddHours(-2)
		};

		await _userActivityRepository.SaveUserActivity(userParticipationNotification, Guid.NewGuid());

		dbSet.Verify(x => x.Update(existingUserActivity), Times.Once);
		_dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		Assert.Equal(oldTimesSeen + 1, existingUserActivity.TimesSeen);
		Assert.Equal(oldLastJoinDate, existingUserActivity.LastJoinDate);
		Assert.Equal(userName, existingUserActivity.Username);
		Assert.Equal(oldStreamSessionId, existingUserActivity.StreamSession);
		Assert.Equal(oldMessagesSent, existingUserActivity.MessagesSent);
	}

	[Fact]
	public async Task SaveUserChatActivity_NullUser()
	{
		var userActivities = Enumerable.Empty<UserActivity>();

		var dbSet = new Mock<DbSet<UserActivity>>();
		_dbContext.Setup(x => x.UserActivities).ReturnsDbSet(userActivities, dbSet);

		await _userActivityRepository.SaveUserChatActivity("someUsername");

		_dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SaveUserChatActivity_ValidUser_MessagesSentIsUpdated()
	{
		var oldLastJoinDate = DateTime.Now;
		const string userName = "calledude";
		const int oldMessagesSent = 2;
		var oldStreamSessionId = Guid.NewGuid();
		const int oldTimesSeen = 4;
		var existingUserActivity = new UserActivity
		{
			LastJoinDate = oldLastJoinDate,
			Username = userName,
			MessagesSent = oldMessagesSent,
			StreamSession = oldStreamSessionId,
			TimesSeen = oldTimesSeen
		};

		var userActivities = new List<UserActivity>
		{
			existingUserActivity
		};

		var dbSet = new Mock<DbSet<UserActivity>>();
		_dbContext.Setup(x => x.UserActivities).ReturnsDbSet(userActivities, dbSet);

		await _userActivityRepository.SaveUserChatActivity(userName);

		_dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		Assert.Equal(oldTimesSeen, existingUserActivity.TimesSeen);
		Assert.Equal(oldLastJoinDate, existingUserActivity.LastJoinDate);
		Assert.Equal(userName, existingUserActivity.Username);
		Assert.Equal(oldStreamSessionId, existingUserActivity.StreamSession);
		Assert.Equal(oldMessagesSent + 1, existingUserActivity.MessagesSent);
	}

	[Fact]
	public async Task GetUserActivity_ReturnsCorrectUser()
	{
		const string userName = "calledude";
		var userActivity1 = new UserActivity
		{
			Username = userName,
		};

		var userActivity2 = new UserActivity
		{
			Username = "someOtherUser"
		};

		var userActivities = new List<UserActivity>
		{
			userActivity1,
			userActivity2
		};

		var dbSet = new Mock<DbSet<UserActivity>>();
		_dbContext.Setup(x => x.UserActivities).ReturnsDbSet(userActivities, dbSet);

		var actualUserActivity = await _userActivityRepository.GetUserActivity(userName);

		Assert.Equal(userActivity1, actualUserActivity);
	}
}
