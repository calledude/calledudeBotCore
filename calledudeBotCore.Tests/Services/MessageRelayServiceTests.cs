using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class MessageRelayServiceTests
{
	private readonly Logger<MessageRelayService> _logger = new(NullLoggerFactory.Instance);
	private readonly IRelayState _relayState;
	private readonly ISteamBot _steam;
	private readonly IOsuBot _osu;
	private readonly ITwitchUser _twitch;
	private readonly ITwitchUserConfig _twitchConfig;
	private const string _broadcaster = "calledude";
	private const string _username = "someUser";

	public MessageRelayServiceTests()
	{
		_relayState = Substitute.For<IRelayState>();
		_steam = Substitute.For<ISteamBot>();
		_osu = Substitute.For<IOsuBot>();
		_twitch = Substitute.For<ITwitchUser>();
		_twitchConfig = Substitute.For<ITwitchUserConfig>();

		_twitchConfig.TwitchChannel.Returns($"#{_broadcaster}");
	}

	[Fact]
	public async Task CommandDoesNotGetRelayed()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState, _twitch, _steam, _osu, _twitchConfig);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch, MessageObjectMother.CreateWithContent("!test"));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		Assert.Empty(_relayState.ReceivedCalls());
		Assert.Empty(_twitch.ReceivedCalls());
		Assert.Empty(_steam.ReceivedCalls());
		Assert.Empty(_osu.ReceivedCalls());
	}

	[Fact]
	public async Task BroadcasterMessageDoesNotGetRelayed()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState, _twitch, _steam, _osu, _twitchConfig);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch, MessageObjectMother.CreateWithContent("hello", _broadcaster));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		Assert.Empty(_relayState.ReceivedCalls());
		Assert.Empty(_twitch.ReceivedCalls());
		Assert.Empty(_steam.ReceivedCalls());
		Assert.Empty(_osu.ReceivedCalls());
	}

	[Fact]
	public async Task MessageIsRelayed_FromTwitch_ToSteamAndOsu()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState, _twitch, _steam, _osu, _twitchConfig);

		var relayNotification = new RelayNotification<IrcMessage>(_twitch, MessageObjectMother.CreateWithContent("hello", _username));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		await _steam.Received(1).SendMessageAsync(Arg.Any<IrcMessage>());
		await _osu.Received(1).SendMessageAsync(Arg.Any<IrcMessage>());
		_ = _osu.Received(1).Name;
		_ = _steam.Received(1).Name;
		_ = _twitch.Received(2).Name;
		_ = _relayState.Received(1).LastMessage;
		_relayState.Received(1).LastMessage = Arg.Any<DateTime>();

		Assert.Equal(2, _steam.ReceivedCalls().Count());
		Assert.Equal(2, _twitch.ReceivedCalls().Count());
		Assert.Equal(2, _osu.ReceivedCalls().Count());
		Assert.Equal(2, _relayState.ReceivedCalls().Count());
	}

	[Fact]
	public async Task MessageIsRelayed_FromSteam_ToTwitch()
	{
		var messageRelayService = new MessageRelayService(_logger, _relayState, _twitch, _steam, _osu, _twitchConfig);

		var relayNotification = new RelayNotification<IrcMessage>(_steam, MessageObjectMother.CreateWithContent("hello", _username));
		await messageRelayService.Handle(relayNotification, CancellationToken.None);

		await _twitch.Received(1).SendMessageAsync(Arg.Any<IrcMessage>());
		_ = _steam.Received(1).Name;
		_ = _twitch.Received(1).Name;
		_ = _relayState.Received(1).LastMessage;
		_relayState.LastMessage = Arg.Any<DateTime>();

		Assert.Single(_steam.ReceivedCalls());
		Assert.Equal(2, _twitch.ReceivedCalls().Count());
		Assert.Empty(_osu.ReceivedCalls());
		Assert.Equal(2, _relayState.ReceivedCalls().Count());
	}
}