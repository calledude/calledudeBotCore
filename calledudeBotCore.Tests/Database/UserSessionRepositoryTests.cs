using calledudeBot.Database;
using calledudeBot.Database.Activity;
using calledudeBot.Database.Session;
using MockQueryable.NSubstitute;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Database;

public class UserSessionRepositoryTests
{
    private readonly DatabaseContext _dbContext;
    private readonly UserSessionRepository _userSessionRepository;

    public UserSessionRepositoryTests()
    {
        _dbContext = Substitute.For<DatabaseContext>();
        _userSessionRepository = new UserSessionRepository(_dbContext);
    }

    [Fact]
    public async Task UserSessionIsTrackedProperly()
    {
        UserSession? actualEntity = null;
        var dbSet = Enumerable.Empty<UserSession>().AsQueryable().BuildMockDbSet();
        await dbSet.AddAsync(Arg.Do<UserSession>(x => actualEntity = x), Arg.Any<CancellationToken>());

        _dbContext.UserSession.Returns(dbSet);

        var now = DateTime.Now;
        var userActivityEntity = new UserActivity
        {
            Username = "calledude",
            LastJoinDate = now.AddMinutes(-5)
        };

        await _userSessionRepository.TrackUserSession(userActivityEntity);

        await dbSet.Received(1).AddAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

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

        var dbSet = userActivities.AsQueryable().BuildMockDbSet();
        _dbContext.UserSession.Returns(dbSet);

        var actualUserSessions = await _userSessionRepository.GetUserSessions(userName);

        Assert.Equal(2, actualUserSessions.Count);
        Assert.Contains(userSession1, actualUserSessions);
        Assert.Contains(userSession2, actualUserSessions);
    }
}
