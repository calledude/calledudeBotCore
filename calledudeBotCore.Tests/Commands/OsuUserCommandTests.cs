using calledudeBot.Chat.Commands;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class OsuUserCommandTests
{
    private readonly Mock<IOsuUserService> _osuUserServiceMock;
    private readonly Logger<OsuUserCommand> _logger = new(NullLoggerFactory.Instance);

    public OsuUserCommandTests()
    {
        _osuUserServiceMock = new Mock<IOsuUserService>();
    }

    [Fact]
    public async Task UsernameIsRequired()
    {
        var osuUserCommand = new OsuUserCommand(_osuUserServiceMock.Object, _logger);
        var commandParameter = CommandParameterObjectMother.EmptyWithPrefixedWord;

        var response = await osuUserCommand.Handle(commandParameter);

        Assert.Equal("Username required.", response);

        _osuUserServiceMock.Verify(x => x.GetOsuUser(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExceptionMessageIsReturnedToUser()
    {
        _osuUserServiceMock
            .Setup(x => x.GetOsuUser(It.IsAny<string>()))
            .Throws<Exception>();

        var osuUserCommand = new OsuUserCommand(_osuUserServiceMock.Object, _logger);
        var commandParameter = CommandParameterObjectMother.CreateWithMessageContent("user");
        var response = await osuUserCommand.Handle(commandParameter);

        Assert.Equal("An error occured while getting osu! user data. Try again later.", response);
        _osuUserServiceMock.Verify(x => x.GetOsuUser(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CouldNotFindUser()
    {
        _osuUserServiceMock
            .Setup(x => x.GetOsuUser(It.IsAny<string>()))
            .ReturnsAsync((OsuUser)null);

        var osuUserCommand = new OsuUserCommand(_osuUserServiceMock.Object, _logger);

        const string userName = "SomeUser";
        var commandParameter = CommandParameterObjectMother.CreateWithMessageContent(userName);
        var response = await osuUserCommand.Handle(commandParameter);

        Assert.Equal($"Could not find user {userName}", response);

        _osuUserServiceMock.Verify(x => x.GetOsuUser(It.Is<string>(y => y == userName)), Times.Once);
        _osuUserServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FoundUser()
    {
        var osuUser = OsuUserObjectMother.GetOsuUser();

        _osuUserServiceMock
            .Setup(x => x.GetOsuUser(It.IsAny<string>()))
            .ReturnsAsync(osuUser);

        var osuUserCommand = new OsuUserCommand(_osuUserServiceMock.Object, _logger);

        const string userName = "calledude";
        var commandParameter = CommandParameterObjectMother.CreateWithMessageContent(userName);
        var response = await osuUserCommand.Handle(commandParameter);

        Assert.Equal($"osu! player {osuUser.Username} - Lv. {osuUser.Level} has 4141.41PP (Global: #42069 - Country: #1337) with 99.95% accuracy", response);

        _osuUserServiceMock.Verify(x => x.GetOsuUser(It.Is<string>(y => y == userName)), Times.Once);
        _osuUserServiceMock.VerifyNoOtherCalls();
    }
}
