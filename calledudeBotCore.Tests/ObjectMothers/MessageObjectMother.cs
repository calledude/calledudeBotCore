using calledudeBot.Chat;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class MessageObjectMother
{
	public static IrcMessage Empty { get; } = new()
	{
		Content = string.Empty,
		Channel = string.Empty,
		Sender = UserObjectMother.Empty
	};

	public static IrcMessage EmptyMod { get; set; } = new()
	{
		Channel = string.Empty,
		Content = string.Empty,
		Sender = UserObjectMother.EmptyMod
	};

	public static IrcMessage CreateWithContent(string content, string? username = null)
		=> new()
		{
			Content = content,
			Channel = string.Empty,
			Sender = new User(username ?? string.Empty)
		};

	public static DiscordMessage CreateDiscordMessageWithContent(string content)
		=> CreateDiscordMessage(content, 0);

	public static DiscordMessage CreateDiscordMessage(string content, ulong id)
		=> new()
		{
			Content = content,
			Channel = string.Empty,
			Destination = id,
			Sender = UserObjectMother.Empty
		};

	public static IrcMessage CreateEmptyWithUser(string userName)
		=> new()
		{
			Content = string.Empty,
			Channel = string.Empty,
			Sender = UserObjectMother.Create(userName)
		};

	public static IrcMessage CreateEmptyWithUser(User user)
		=> new()
		{
			Content = string.Empty,
			Channel = string.Empty,
			Sender = user
		};
}
