using calledudeBot.Database;
using calledudeBot.Database.Activity;
using calledudeBot.Models;
using calledudeBotCore.Tests.ObjectMothers;
using MockQueryable.NSubstitute;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Database;

public class UserActivityRepositoryTests
{
	private readonly DatabaseContext _dbContext;
	private readonly UserActivityRepository _userActivityRepository;

	public UserActivityRepositoryTests()
	{
		_dbContext = Substitute.For<DatabaseContext>();
		_userActivityRepository = new UserActivityRepository(_dbContext);
	}

	[Fact]
	public async Task SavingNewUserActivity()
	{
		var userActivities = Enumerable.Empty<UserActivity>();

		UserActivity? actualEntity = null;

		var dbSet = userActivities.AsQueryable().BuildMockDbSet();
		await dbSet.AddAsync(Arg.Do<UserActivity>(x => actualEntity = x), Arg.Any<CancellationToken>());

		_dbContext.UserActivities.Returns(dbSet);

		var now = DateTime.UtcNow;
		const string userName = "calledude";
		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Create(userName), ParticipationType.Leave)
		{
			When = now
		};

		var streamSessionId = Guid.NewGuid();
		await _userActivityRepository.SaveUserActivity(userParticipationNotification, streamSessionId);

		Assert.NotNull(actualEntity);
		Assert.Equal(now, actualEntity.LastJoinDate);
		Assert.Equal(userName, actualEntity.Username);
		Assert.Equal(1, actualEntity.TimesSeen);
		Assert.Equal(streamSessionId, actualEntity.StreamSession);

		await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdatingExistingUserActivity()
	{
		var oldLastJoinDate = DateTime.UtcNow;
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

		var dbSet = userActivities.AsQueryable().BuildMockDbSet();
		_dbContext.UserActivities.Returns(dbSet);

		var userParticipationNotification = new UserParticipationNotification(UserObjectMother.Create(userName), ParticipationType.Leave)
		{
			When = DateTime.UtcNow.AddHours(-2)
		};

		await _userActivityRepository.SaveUserActivity(userParticipationNotification, Guid.NewGuid());

		dbSet.Received(1).Update(existingUserActivity);
		await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

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

		var dbSet = userActivities.AsQueryable().BuildMockDbSet();
		_dbContext.UserActivities.Returns(dbSet);

		await _userActivityRepository.SaveUserChatActivity("someUsername");

		await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SaveUserChatActivity_ValidUser_MessagesSentIsUpdated()
	{
		var oldLastJoinDate = DateTime.UtcNow;
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

		var dbSet = userActivities.AsQueryable().BuildMockDbSet();
		_dbContext.UserActivities.Returns(dbSet);

		await _userActivityRepository.SaveUserChatActivity(userName);

		await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

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

		var dbSet = userActivities.AsQueryable().BuildMockDbSet();
		_dbContext.UserActivities.Returns(dbSet);

		var actualUserActivity = await _userActivityRepository.GetUserActivity(userName);

		Assert.Equal(userActivity1, actualUserActivity);
	}
}