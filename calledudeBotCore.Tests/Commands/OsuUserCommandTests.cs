using calledudeBot.Chat.Commands;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Commands;

public class OsuUserCommandTests
{
	private readonly IOsuUserService _osuUserServiceMock;
	private readonly Logger<OsuUserCommand> _logger = new(NullLoggerFactory.Instance);

	public OsuUserCommandTests()
	{
		_osuUserServiceMock = Substitute.For<IOsuUserService>();
	}

	[Fact]
	public async Task UsernameIsRequired()
	{
		var osuUserCommand = new OsuUserCommand(_osuUserServiceMock, _logger);
		var commandParameter = CommandParameterObjectMother.EmptyWithPrefixedWord;

		var response = await osuUserCommand.Handle(commandParameter);

		Assert.Equal("Username required.", response);

		await _osuUserServiceMock.DidNotReceive().GetOsuUser(Arg.Any<string>());
	}

	[Fact]
	public async Task ExceptionMessageIsReturnedToUser()
	{
		_osuUserServiceMock
			.GetOsuUser(Arg.Any<string>())
			.Throws<Exception>();

		var osuUserCommand = new OsuUserCommand(_osuUserServiceMock, _logger);
		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent("user");
		var response = await osuUserCommand.Handle(commandParameter);

		Assert.Equal("An error occured while getting osu! user data. Try again later.", response);
		await _osuUserServiceMock.Received(1).GetOsuUser(Arg.Any<string>());
	}

	[Fact]
	public async Task CouldNotFindUser()
	{
		_osuUserServiceMock
			.GetOsuUser(Arg.Any<string>())
			.Returns((OsuUser?)null);

		var osuUserCommand = new OsuUserCommand(_osuUserServiceMock, _logger);

		const string userName = "SomeUser";
		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(userName);
		var response = await osuUserCommand.Handle(commandParameter);

		Assert.Equal($"Could not find user {userName}", response);

		await _osuUserServiceMock.Received(1).GetOsuUser(Arg.Is<string>(y => y == userName));

		Assert.Single(_osuUserServiceMock.ReceivedCalls());
	}

	[Fact]
	public async Task FoundUser()
	{
		var osuUser = OsuUserObjectMother.CreateOsuUser();

		_osuUserServiceMock
			.GetOsuUser(Arg.Any<string>())
			.Returns(osuUser);

		var osuUserCommand = new OsuUserCommand(_osuUserServiceMock, _logger);

		const string userName = "calledude";
		var commandParameter = CommandParameterObjectMother.CreateWithPrefixedMessageContent(userName);
		var response = await osuUserCommand.Handle(commandParameter);

		Assert.Equal($"osu! player {osuUser.Username} - Lv. {osuUser.Level} has 4141.41PP (Global: #42069 - Country: #1337) with 99.95% accuracy", response);

		await _osuUserServiceMock.Received(1).GetOsuUser(Arg.Is<string>(y => y == userName));
		Assert.Single(_osuUserServiceMock.ReceivedCalls());
	}
}