using calledudeBot.Chat;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class MessageObjectMother
{
	public static IrcMessage Empty { get; } = new IrcMessage(string.Empty, string.Empty, UserObjectMother.Empty);
	public static IrcMessage EmptyMod { get; set; } = new IrcMessage(string.Empty, string.Empty, UserObjectMother.EmptyMod);

	public static IrcMessage CreateWithContent(string content, string? username = null)
		=> new(content, string.Empty, new User(username ?? string.Empty));

	public static DiscordMessage CreateDiscordMessageWithContent(string content)
		=> CreateDiscordMessage(content, 0);

	public static DiscordMessage CreateDiscordMessage(string content, ulong id)
	=> new(content, string.Empty, UserObjectMother.Empty, id);

	public static IrcMessage CreateEmptyWithUser(string userName)
		=> new(string.Empty, string.Empty, UserObjectMother.Create(userName));

	public static IrcMessage CreateEmptyWithUser(User user)
	=> new(string.Empty, string.Empty, user);
}
