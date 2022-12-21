using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class SongRequestServiceTests
{
    [Fact]
    public async Task NotASongRequest()
    {
        var clientFactory = new Mock<IHttpClientWrapper>();

        var options = ConfigObjectMother.Create();
        var target = new SongRequestService(options, null!, null!, clientFactory.Object, null!);

        await target.Handle(MessageObjectMother.Empty, CancellationToken.None);
        clientFactory.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://osu.ppy.sh/b/12345", "12345", "someUser1", "Artist1", "Title1", "Version1")]
    [InlineData("https://osu.ppy.sh/b/456321", "456321", "someUser2", "Artist2", "Title2", "Version2")]
    [InlineData("https://osu.ppy.sh/beatmapsets/362039#osu/1337", "1337", "someUser3", "Artist3", "Title3", "Version3")]
    [InlineData("Play this song! https://osu.ppy.sh/beatmapsets/362039#osu/420", "420", "someUser4", "Artist4", "Title4", "Version4")]
    [InlineData("Play this song! http://osu.ppy.sh/beatmapsets/362039#osu/696969", "696969", "someUser5", "Artist5", "Title5", "Version5")]
    [InlineData("Play this song! http://osu.ppy.sh/beatmapsets/362039#osu/42069 or else", "42069", "someUser6", "Artist6", "Title6", "Version6")]
    [InlineData("http://osu.ppy.sh/beatmapsets/362039#osu/111 play this!", "111", "someUser6", "Artist6", "Title6", "Version6")]
    public async Task FoundSong(string messageContent, string beatmapId, string username, string songArtist, string songTitle, string songVersion)
    {
        string? actualUrl = null;
        var osuSong = new OsuSong
        {
            BeatmapVersion = songVersion,
            Artist = songArtist,
            Title = songTitle,
        };

        var clientFactory = new Mock<IHttpClientWrapper>();
        clientFactory
            .Setup(x => x.GetAsJsonAsync<OsuSong[]>(It.IsAny<string>()))
            .ReturnsAsync((true, new[] { osuSong }))
            .Callback((string url) => actualUrl = url);

        string? sentMessageContent = null;
        var osu = new Mock<IOsuBot>();
        osu
            .Setup(x => x.SendMessageAsync(It.IsAny<IrcMessage>()))
            .Callback((IrcMessage message) => sentMessageContent = message.Content);

        var logger = LoggerObjectMother.NullLoggerFor<SongRequestService>();
        var options = ConfigObjectMother.Create();
        var target = new SongRequestService(options, null!, osu.Object, clientFactory.Object, logger);

        var message = MessageObjectMother.CreateWithContent(messageContent, username);
        await target.Handle(message, CancellationToken.None);

        clientFactory.Verify(x => x.GetAsJsonAsync<OsuSong[]>(It.IsAny<string>()), Times.Once);
        clientFactory.VerifyNoOtherCalls();
        osu.Verify(x => x.SendMessageAsync(It.IsAny<IrcMessage>()), Times.Once);
        osu.VerifyNoOtherCalls();

        var expectedMessage = $"{username} requested song: [https://osu.ppy.sh/b/{beatmapId} {osuSong.Artist} - {osuSong.Title} [{osuSong.BeatmapVersion}]]";
        Assert.Equal(expectedMessage, sentMessageContent);
        Assert.StartsWith("https://osu.ppy.sh/api/get_beatmaps", actualUrl);
        Assert.EndsWith($"?k={options.Value.OsuAPIToken}&b={beatmapId}", actualUrl);
    }

    [Fact]
    public async Task NonSuccessfulRequest_Returns()
    {
        var clientFactory = new Mock<IHttpClientWrapper>();

        var logger = LoggerObjectMother.NullLoggerFor<SongRequestService>();
        var options = ConfigObjectMother.Create();
        var target = new SongRequestService(options, null!, null!, clientFactory.Object, logger);

        var message = MessageObjectMother.CreateWithContent("http://osu.ppy.sh/b/12345");
        await target.Handle(message, CancellationToken.None);

        clientFactory.Verify(x => x.GetAsJsonAsync<OsuSong[]>(It.IsAny<string>()), Times.Once);
        clientFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulRequest_NoSongHits()
    {
        var clientFactory = new Mock<IHttpClientWrapper>();
        clientFactory
            .Setup(x => x.GetAsJsonAsync<OsuSong[]>(It.IsAny<string>()))
            .ReturnsAsync((true, null));

        string? sentMessageContent = null;
        var twitch = new Mock<ITwitchBot>();
        twitch
            .Setup(x => x.SendMessageAsync(It.IsAny<IrcMessage>()))
            .Callback((IrcMessage message) => sentMessageContent = message.Content);

        var options = ConfigObjectMother.Create();
        var target = new SongRequestService(options, twitch.Object, null!, clientFactory.Object, null!);

        var message = MessageObjectMother.CreateWithContent("http://osu.ppy.sh/b/12345");
        await target.Handle(message, CancellationToken.None);

        clientFactory.Verify(x => x.GetAsJsonAsync<OsuSong[]>(It.IsAny<string>()), Times.Once);
        clientFactory.VerifyNoOtherCalls();

        Assert.Equal("I couldn't find that song, sorry.", sentMessageContent);
    }
}
