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
using Moq;
using System;
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
		var client = new Mock<IHttpClientWrapper>();
		var hostedService = new Mock<IHostedService>();
		var readyNotification = new ReadyNotification(hostedService.Object);

		var timer = new Mock<IAsyncTimer>();

		var target = new OsuUserService(client.Object, _config, null!, _logger, timer.Object);
		await target.Handle(readyNotification, CancellationToken.None);

		timer.Verify(x => x.Start(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData(null, "osuUsername", false)]
	[InlineData("calledude", "calledude", false)]
	[InlineData("calledude", "calledude", true)]
	public async Task GetOsuUser(string username, string requestUsername, bool success)
	{
		string? actualUrl = null;
		var client = new Mock<IHttpClientWrapper>();
		client
			.Setup(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()))
			.ReturnsAsync((success, new OsuUser[] { OsuUserObjectMother.CreateOsuUser() }))
			.Callback((string url) => actualUrl = url);

		var target = new OsuUserService(client.Object, _config, null!, _logger, new Mock<IAsyncTimer>().Object);
		var result = await target.GetOsuUser(username);

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
		var client = new Mock<IHttpClientWrapper>();
		client
			.Setup(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()))
			.ReturnsAsync((true, new OsuUser[] { osuUser, osuUser }));

		var target = new OsuUserService(client.Object, _config, null!, _logger, new Mock<IAsyncTimer>().Object);
		await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetOsuUser(""));
	}

	[Fact]
	public async Task GetOsuUser_RetrievesNullArray_Throws()
	{
		var client = new Mock<IHttpClientWrapper>();
		client
			.Setup(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()))
			.ReturnsAsync((true, null));

		var target = new OsuUserService(client.Object, _config, null!, _logger, new Mock<IAsyncTimer>().Object);
		var ex = await Assert.ThrowsAsync<Exception>(() => target.GetOsuUser(""));

		Assert.Equal("Response from osu! was null (possibly invalid username)", ex.Message);
	}

	[Theory]
	[InlineData(42069, 4141.41f, 32069, 5151.51f, "calledude just gained 10000 ranks (#32069). PP: +1010.10pp (5151.51pp)", 1)]
	[InlineData(32069, 5151.51f, 42069, 4141.41f, "calledude just lost 10000 ranks (#42069). PP: -1010.10pp (4141.41pp)", 1)]
	[InlineData(42069, 4141.5f, 42069, 4141.41f, null, 0)]
	[InlineData(42069, 4141.41f, 42069, 4141.5f, null, 0)]
	public async Task Rank_PP_Calculations(int oldRank, float oldPP, int newRank, float newPP, string expectedOutput, int timesCalled)
	{
		var oldOsuUser = OsuUserObjectMother.CreateOsuUser(rank: oldRank, pp: oldPP);
		var newOsuUser = OsuUserObjectMother.CreateOsuUser(rank: newRank, pp: newPP);

		var client = new Mock<IHttpClientWrapper>();
		client
			.SetupSequence(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()))
			.ReturnsAsync((true, new OsuUser[] { oldOsuUser }))
			.ReturnsAsync((true, new OsuUser[] { newOsuUser }));

		string? sentMessageContent = null;

		var twitchMock = new Mock<ITwitchBot>();
		twitchMock
			.Setup(x => x.SendMessageAsync(It.IsAny<IrcMessage>()))
			.Callback((IrcMessage message) => sentMessageContent = message.Content);

		var timerMock = new Mock<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock
			.Setup(x => x.Start(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Callback((Func<CancellationToken, Task> timerCallback, CancellationToken _) => callback = timerCallback);

		var osuUserService = new OsuUserService(client.Object, _config, twitchMock.Object, _logger, timerMock.Object);
		var readyNotification = new ReadyNotification(twitchMock.Object);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		// Invoke twice to consume all the mocked data for GetAsJsonAsync
		for (var i = 0; i < 2; ++i)
		{
			await callback!.Invoke(CancellationToken.None);
		}

		twitchMock.Verify(x => x.SendMessageAsync(It.IsAny<IrcMessage>()), Times.Exactly(timesCalled));
		twitchMock.VerifyNoOtherCalls();

		Assert.Equal(expectedOutput, sentMessageContent);
	}

	[Fact]
	public async Task CheckUserUpdate_RespectsCancellationToken()
	{
		var client = new Mock<IHttpClientWrapper>();
		var twitchMock = new Mock<ITwitchBot>();
		var timerMock = new Mock<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock
			.Setup(x => x.Start(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Callback((Func<CancellationToken, Task> timerCallback, CancellationToken _) => callback = timerCallback);

		var osuUserService = new OsuUserService(client.Object, _config, twitchMock.Object, _logger, timerMock.Object);
		var readyNotification = new ReadyNotification(twitchMock.Object);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		var cts = new CancellationTokenSource();
		cts.Cancel();
		await callback!.Invoke(cts.Token);

		client.VerifyNoOtherCalls();
		twitchMock.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task CheckUserUpdate_OsuUserDataUnavailable_Bails()
	{
		var client = new Mock<IHttpClientWrapper>();
		client
			.Setup(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()))
			.ReturnsAsync((false, null));

		var twitchMock = new Mock<ITwitchBot>();
		var timerMock = new Mock<IAsyncTimer>();

		Func<CancellationToken, Task>? callback = null;
		timerMock
			.Setup(x => x.Start(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Callback((Func<CancellationToken, Task> timerCallback, CancellationToken _) => callback = timerCallback);

		var osuUserService = new OsuUserService(client.Object, _config, twitchMock.Object, _logger, timerMock.Object);
		var readyNotification = new ReadyNotification(twitchMock.Object);
		await osuUserService.Handle(readyNotification, CancellationToken.None);

		Assert.NotNull(callback);

		await callback!.Invoke(CancellationToken.None);

		client.Verify(x => x.GetAsJsonAsync<OsuUser[]>(It.IsAny<string>()), Times.Once);
		client.VerifyNoOtherCalls();
		twitchMock.VerifyNoOtherCalls();
	}
}
