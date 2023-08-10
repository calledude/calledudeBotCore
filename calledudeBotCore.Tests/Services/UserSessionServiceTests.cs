using calledudeBot.Database.Session;
using calledudeBot.Services;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class UserSessionServiceTests
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly UserSessionService _userSessionService;

    public UserSessionServiceTests()
    {
        _userSessionRepository = Substitute.For<IUserSessionRepository>();
        _userSessionService = new UserSessionService(_userSessionRepository);
    }

    [Fact]
    public async Task GetUserSession_Calls_Repository()
    {
        const string username = "calledude";
        await _userSessionService.GetUserSessions(username);

        await _userSessionRepository.Received(1).GetUserSessions(username);
        Assert.Single(_userSessionRepository.ReceivedCalls());
    }
}
