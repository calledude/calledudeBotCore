using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Config;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBot.Utilities;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class OsuUserServiceTests
{
	private static readonly IOptions<BotConfig> _config = ConfigObjectMother.Create();
	private static readonly Logger<OsuUserService> _logger = LoggerObjectMother.NullLoggerFor<OsuUserService>();

	[Fact]
	public async Task DoesNotStartIfNotTwitchBot()
	{
		var client = Substitute.For<IHttpClientWrapper>();
		var hostedService = Substitute.For<IHostedService>();
		var readyNotification = new ReadyNotification(hostedService);

		var timer = Substitute.For<IAsyncTimer>();

		var target = new OsuUserService(client, _config, null!, _logger, timer);
		await target.Handle(readyNotification, CancellationToken.None);

		timer.DidNotReceive().Start(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
	}

	[Theory]
	[InlineData(null, "osuUsername", false)]
	[InlineData("calledude", "calledude", false)]
	[InlineData("calledude", "calledude", true)]
	public async Task GetOsuUser(string? username, string requestUsername, bool success)
	{
		string? actualUrl = null;
		var client = Substitute.For<IHttpClientWrapper>();
		client
			.GetAsJsonAsync(Arg.Do<string>(x => actualUrl = x), Arg.Any<JsonTypeInfo<OsuUser[]>>())
			.Returns((success, new OsuUser[] { OsuUserObjectMother.CreateOsuUser() }));

		var target = new OsuUserService(client, _config, null!, _logger, Substitute.For<IAsyncTimer>());
		var result = await target.GetOsuUser(username!);

		if (success)
		{
			Assert.NotNull(result);
		}
		else
		{
			Assert.Null(result);
		}

		Assert.StartsWith("https://osu.ppy.sh/api/get_user", actualUrl);
		Assert.EndsWith($"?k={_config.Value.OsuAPIToken}&u={requestUsername}", actualUrl);
	}

	[Fact]
	public async Task GetOsuUser_RetrievesMoreThanOneResult_Throws()
	{
		var osuUser = OsuUserObjectMother.CreateOsuUser();
		var client = Substitute.For<IHttpClientWrapper>();
		client
			.GetAsJsonAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<OsuUser[]>>())
			.Returns((true, new OsuUser[] { osuUser, osuUser }));

		var target = new OsuUserService(client, _config, null!, _logger, Substitute.For<IAsyncTimer>());
		await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetOsuUser(""));
	}

	[Fact]
	public async Task GetOsuUser_RetrievesNullArray_Throws()
	{
		var client = Substitute.For<IHttpClientWrapper>();
		client
			.GetAsJsonAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<OsuUser[]>>())
			.Returns((true, null));

		var target = new OsuUserService(client, _config, null!, _logger, Substitute.For<IAsyncTimer>());
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetOsuUser(""));

		Assert.Equal("Response from osu! was null (possibly invalid username)", ex.Message);
	}

	[Theory]
	[InlineData(42069, 4141.41f, 32069, 5151.51f, "calledude just gained 10000 ranks (#32069). PP: +1010.10pp (5151.51pp)", 1)]
	[InlineData(32069, 5151.51f, 42069, 4141.41f, "calledude just lost 10000 ranks (#42069). PP: -1010.10pp (4141.41pp)", 1)]
	[InlineData(42069, 4141.5f, 42069, 4141.41f, null, 0)]
	[InlineData(42069, 4141.41f, 42069, 4141.5f, null, 0)]
	public async Task Rank_PP_Calculations(int oldRank, float oldPP, int newRank, float newPP, string? expectedOutput, int timesCalled)
	{
		var oldOsuUser = OsuUserObjectMother.CreateOsuUser(rank: oldRank, pp: oldPP);
		var newOsuUser = OsuUserObjectMother.CreateOsuUser(rank: newRank, pp: newPP);

		var client = Substitute.For<IHttpClientWrapper>();
		client
			.GetAsJsonAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<OsuUser[]>>())
			.Returns
			(
				(true, new OsuUser[] { oldOsuUser }),
				(true, new OsuUser[] { newOsuUser })
			);

		string? sentMessageContent = null;

		var twitchMock = Substitute.For<ITwitchBot>();
		await twitchMock.SendMessageAsync(Arg.Do<IrcMessage>(x => sentMessageContent = x.Content));

		var timerMock = Substitute.For<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock.Start(
			Arg.Do<Func<CancellationToken, Task>>(x => callback = x),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>());

		var osuUserService = new OsuUserService(client, _config, twitchMock, _logger, timerMock);
		var readyNotification = new ReadyNotification(twitchMock);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		// Invoke twice to consume all the mocked data for GetAsJsonAsync
		for (var i = 0; i < 2; ++i)
		{
			await callback!.Invoke(CancellationToken.None);
		}

		await twitchMock.Received(timesCalled).SendMessageAsync(Arg.Any<IrcMessage>());
		Assert.Equal(timesCalled, twitchMock.ReceivedCalls().Count());

		Assert.Equal(expectedOutput, sentMessageContent);
	}

	[Fact]
	public async Task CheckUserUpdate_RespectsCancellationToken()
	{
		var client = Substitute.For<IHttpClientWrapper>();
		var twitchMock = Substitute.For<ITwitchBot>();
		var timerMock = Substitute.For<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock.Start(
			Arg.Do<Func<CancellationToken, Task>>(x => callback = x),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>());

		var osuUserService = new OsuUserService(client, _config, twitchMock, _logger, timerMock);
		var readyNotification = new ReadyNotification(twitchMock);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		using var cts = new CancellationTokenSource();
		cts.Cancel();
		await callback!.Invoke(cts.Token);

		Assert.Empty(client.ReceivedCalls());
		Assert.Empty(twitchMock.ReceivedCalls());
	}

	[Fact]
	public async Task CheckUserUpdate_OsuUserDataUnavailable_Bails()
	{
		var client = Substitute.For<IHttpClientWrapper>();
		client
			.GetAsJsonAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<OsuUser[]>>())
			.Returns((false, null));

		var twitchMock = Substitute.For<ITwitchBot>();
		var timerMock = Substitute.For<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock.Start(
			Arg.Do<Func<CancellationToken, Task>>(x => callback = x),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>());

		var osuUserService = new OsuUserService(client, _config, twitchMock, _logger, timerMock);
		var readyNotification = new ReadyNotification(twitchMock);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		await callback!.Invoke(CancellationToken.None);

		await client.Received(1).GetAsJsonAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<OsuUser[]>>());
		Assert.Single(client.ReceivedCalls());
		Assert.Empty(twitchMock.ReceivedCalls());
	}
}