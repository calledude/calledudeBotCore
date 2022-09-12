namespace calledudeBot.Chat;

public sealed record DiscordMessage : Message
{
	public ulong Destination { get; init; }
}
