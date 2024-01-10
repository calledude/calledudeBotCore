using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Config;

namespace calledudeBot.Bots;

public interface ITwitchUser : IMessageBot<IrcMessage>;

public class TwitchUser : TwitchBotBase, ITwitchUser
{
	public TwitchUser(IIrcClient ircClient, ITwitchUserConfig config) : base(ircClient, config)
	{
	}

	public override string Name => "Twitch";
}