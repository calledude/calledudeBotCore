using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class MessageRelayServiceTests
{
	private readonly Logger<MessageRelayService> _logger = new(NullLoggerFactory.Instance);
	private readonly Mock<IRelayState> _relayState;
	private readonly Mock<ISteamBot> _steam;
	private readonly Mock<IOsuBot> _osu;
	private readonly Mock<ITwitchUser> _twitch;
	private readonly Mock<ITwitchUserConfig> _twitchConfig;
	private const string _broadcaster = "calledude";
	private const string _username = "someUser";

	public MessageRelayServiceTests()
	{
		_relayState = new Mock<IRelayState>();
		_steam = new Mock<ISteamBot>();
		_osu = new Mock<IOsuBot>();
		_twitch = new Mock<ITwitchUser>();
		_twitchConfig = new Mock<ITwitchUserConfig>();

		_twitchConfig.SetupGet(x => x.TwitchChannel).Returns($"#{_broadcaster}");
	}

	[Fact]
	public async Task CommandDoesNotGetRelayed()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState.Object, _twitch.Object, _steam.Object, _osu.Object, _twitchConfig.Object);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch.Object, MessageObjectMother.CreateWithContent("!test"));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		_relayState.VerifyNoOtherCalls();
		_twitch.VerifyNoOtherCalls();
		_steam.VerifyNoOtherCalls();
		_osu.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task BroadcasterMessageDoesNotGetRelayed()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState.Object, _twitch.Object, _steam.Object, _osu.Object, _twitchConfig.Object);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch.Object, MessageObjectMother.CreateWithContent("hello", _broadcaster));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		_relayState.VerifyNoOtherCalls();
		_twitch.VerifyNoOtherCalls();
		_steam.VerifyNoOtherCalls();
		_osu.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task MessageIsRelayed_FromTwitch_ToSteam()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState.Object, _twitch.Object, _steam.Object, _osu.Object, _twitchConfig.Object);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch.Object, MessageObjectMother.CreateWithContent("hello", _username));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		_steam.Verify(x => x.SendMessageAsync(It.IsAny<IrcMessage>()), Times.Once);
		_osu.Verify(x => x.SendMessageAsync(It.IsAny<IrcMessage>()), Times.Once);
		_osu.VerifyGet(x => x.Name, Times.Once);
		_steam.VerifyGet(x => x.Name, Times.Once);
		_twitch.VerifyGet(x => x.Name, Times.Exactly(2));
		_relayState.VerifyGet(x => x.LastMessage, Times.Once);
		_relayState.VerifySet(x => x.LastMessage = It.IsAny<DateTime>(), Times.Once);

		_steam.VerifyNoOtherCalls();
		_twitch.VerifyNoOtherCalls();
		_osu.VerifyNoOtherCalls();
		_relayState.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task MessageIsRelayed_FromSteam_ToTwitch()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState.Object, _twitch.Object, _steam.Object, _osu.Object, _twitchConfig.Object);

		var relayNotification = new RelayNotification<IrcMessage>(_steam.Object, MessageObjectMother.CreateWithContent("hello", _username));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		_twitch.Verify(x => x.SendMessageAsync(It.IsAny<IrcMessage>()), Times.Once);
		_steam.VerifyGet(x => x.Name, Times.Once);
		_twitch.VerifyGet(x => x.Name, Times.Once);
		_relayState.VerifyGet(x => x.LastMessage, Times.Once);
		_relayState.VerifySet(x => x.LastMessage = It.IsAny<DateTime>(), Times.Once);

		_steam.VerifyNoOtherCalls();
		_twitch.VerifyNoOtherCalls();
		_osu.VerifyNoOtherCalls();
		_relayState.VerifyNoOtherCalls();
	}
}
