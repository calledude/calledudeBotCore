using calledudeBot.Database.UserSession;
using calledudeBot.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class UserSessionServiceTests
{
	private readonly Mock<IUserSessionRepository> _userSessionRepository;
	private readonly UserSessionService _userSessionService;

	public UserSessionServiceTests()
	{
		_userSessionRepository = new Mock<IUserSessionRepository>();
		_userSessionService = new UserSessionService(_userSessionRepository.Object);
	}

	[Fact]
	public async Task GetUserSession_Calls_Repository()
	{
		const string username = "calledude";
		await _userSessionService.GetUserSessions(username);

		_userSessionRepository.Verify(x => x.GetUserSessions(username), Times.Once);
		_userSessionRepository.VerifyNoOtherCalls();
	}
}
