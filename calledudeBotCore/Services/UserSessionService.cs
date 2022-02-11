using calledudeBot.Database.UserSession;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IUserSessionService
{
    Task<List<UserSessionEntity>> GetUserSessions(string userName);
}

public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _userSessionRepository;

    public UserSessionService(IUserSessionRepository userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    public async Task<List<UserSessionEntity>> GetUserSessions(string userName)
        => await _userSessionRepository.GetUserSessions(userName);
}
