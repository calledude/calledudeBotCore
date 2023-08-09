using calledudeBot.Chat.Info;
using calledudeBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public class StatsCommand : SpecialCommand<CommandParameter>
{
	private readonly IUserSessionService _userSessionService;
	private readonly IUserActivityService _userActivityService;

	public StatsCommand(IUserSessionService userSessionService, IUserActivityService userActivityService)
	{
		Name = "!stats";
		Description = "Gives information about a twitch user";
		RequiresMod = false;

		_userSessionService = userSessionService;
		_userActivityService = userActivityService;
	}

	protected override async Task<string> HandleCommand(CommandParameter param)
	{
		var user = param.Words.FirstOrDefault() ?? param.Message.Sender?.Name;
		var userActivity = await _userActivityService.GetUserActivity(user);

		if (userActivity is null)
			return $"User {user} has no recorded activity.";

		var userSessions = await _userSessionService.GetUserSessions(user);
		var totalWatchTime = userSessions.Aggregate(TimeSpan.Zero, (curr, b) => curr + b.WatchTime, x => x);

		return $"User {user} | Total watchtime: {totalWatchTime} | Seen: {userActivity.TimesSeen} times | Messages: {userActivity.MessagesSent}";
	}
}
