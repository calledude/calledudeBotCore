namespace calledudeBot.Config
{
    public class BotConfig
    {
        public string? DiscordToken { get; init; }
        public ulong StreamerId { get; init; }
        public ulong AnnounceChannelId { get; init; }
        public string? SteamUsername { get; init; }
        public string? SteamPassword { get; init; }
    }

    public interface ITwitchConfig
    {
        public string? TwitchToken { get; init; }
        public string? TwitchChannel { get; init; }
        public string? TwitchBotUsername { get; init; }
        public bool IsUser { get; init; }
    }

    public interface ITwitchUserConfig : ITwitchConfig
    {
    }

    public interface ITwitchBotConfig : ITwitchConfig
    {
    }

    public class TwitchBotConfig : ITwitchBotConfig, ITwitchUserConfig
    {
        public string? TwitchToken { get; init; }
        public string? TwitchChannel { get; init; }
        public string? TwitchBotUsername { get; init; }
        public bool IsUser { get; init; }
    }
}
