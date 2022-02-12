using calledudeBot.Config;
using Microsoft.Extensions.Options;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class ConfigObjectMother
{
    public static IOptions<BotConfig> Create()
        => Options.Create(new BotConfig
        {
            DiscordToken = string.Empty,
            AnnounceChannelId = 123,
            OBSWebsocketPort = 9999,
            OBSWebsocketUrl = "ws://someUrl:4444",
            OsuAPIToken = "osuApiToken",
            OsuIRCToken = "osuIrcToken",
            OsuUsername = "osuUsername",
            SteamPassword = "steamPassword",
            SteamUsername = "steamUsername",
            StreamerId = 1337
        });
}
