using calledudeBot.Chat;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class UserObjectMother
{
	public static User Empty { get; } = new User(string.Empty);
	public static User EmptyMod { get; } = new User(string.Empty, true);

	public static User Create(string userName, bool isMod = false)
		=> new(userName, isMod);
}