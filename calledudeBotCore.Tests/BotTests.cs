using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using calledudeBot.Config;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests
{
    public class BotTests
    {
        private int _counter;

        [Fact]
        public async Task Mods_Only_Evaluates_Once_Per_Context()
        {
            var fakeCommands = Enumerable
                .Range(1, 10)
                .Select(x =>
                {
                    var cmdMock = new Mock<SpecialCommand<CommandParameter>>();

                    cmdMock
                        .SetupGet(x => x.RequiresMod)
                        .Returns(true);

                    cmdMock
                        .SetupGet(x => x.Name)
                        .Returns("!" + x);

                    return cmdMock.Object;
                })
                .Cast<Command>();

            var commandContainer = new Lazy<CommandContainer>(new CommandContainer(fakeCommands));

            var botMock = new Mock<IMessageBot<IrcMessage>>();
            var logger = new Logger<CommandHandler<IrcMessage>>(NullLoggerFactory.Instance);
            var twitchCommandHandler = new CommandHandler<IrcMessage>(logger, botMock.Object, commandContainer);

            var message = new IrcMessage(
                "",
                "",
                new User("", () =>
                {
                    _counter++;
                    return Task.FromResult(true);
                }));

            var commandExecutions = fakeCommands.Select(x =>
            {
                message = message.CloneWithMessage(x.Name);
                return twitchCommandHandler.Handle(message, CancellationToken.None);
            });

            await Task.WhenAll(commandExecutions);

            Assert.Equal(1, _counter);
        }

        [Fact]
        public async Task Mods_Are_Case_Insensitive()
        {
            var ircClient = new IrcClient(new Logger<IrcClient>(NullLoggerFactory.Instance));
            var twitch = new TwitchBot(ircClient, new TwitchBotConfig() { TwitchChannel = "#calledude" }, new Mock<IMessageDispatcher>().Object, new Logger<TwitchBot>(NullLoggerFactory.Instance));
            await twitch.HandleRawMessage(":tmi.twitch.tv NOTICE #calledude :The moderators of this channel are: CALLEDUDE, calLEDuDeBoT");
            var mods = await twitch.GetMods();

            Assert.Contains("calledude", mods);
            Assert.Contains("calledudebot", mods);
        }
    }
}
