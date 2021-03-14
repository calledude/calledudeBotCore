using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests
{
    public class CommandTests
    {
        [Fact]
        public async Task Custom_Command_Only_Alternate_Is_Deleted()
        {
            var commandContainer = new Lazy<CommandContainer>(() => new CommandContainer(Enumerable.Empty<Command>()));
            var deleteCmd = new DeleteCommand(commandContainer);
            commandContainer.Value.Commands.Add(deleteCmd);

            const string alternateName = "!yo";
            var commandToDelete = new Command()
            {
                Name = "!hi",
                Response = "hi :)",
                AlternateName = new List<string> { alternateName }
            };

            commandContainer.Value.Commands.Add(commandToDelete);

            var messageParams = $"{deleteCmd.Name} {alternateName}".Split();
            var response = await deleteCmd.Handle(new CommandParameter<IrcMessage>(messageParams, new IrcMessage("")));

            Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Key == alternateName);
            Assert.Contains(commandContainer.Value.Commands, x => x.Key == commandToDelete.Name);
            Assert.Equal($"Deleted alternative command '{alternateName}'", response);
        }

        [Fact]
        public async Task Custom_Command_And_Alternates_Gets_Deleted_Properly()
        {
            var commandContainer = new Lazy<CommandContainer>(() => new CommandContainer(Enumerable.Empty<Command>()));
            var deleteCmd = new DeleteCommand(commandContainer);
            commandContainer.Value.Commands.Add(deleteCmd);

            const string alternateName = "!yo";
            var commandToDelete = new Command()
            {
                Name = "!hi",
                Response = "hi :)",
                AlternateName = new List<string> { alternateName }
            };

            commandContainer.Value.Commands.Add(commandToDelete);

            var messageParams = $"{deleteCmd.Name} {commandToDelete.Name}".Split();
            var response = await deleteCmd.Handle(new CommandParameter<IrcMessage>(messageParams, new IrcMessage("")));

            Assert.DoesNotContain(commandContainer.Value.Commands, x => x.Key == "!hi" || x.Key == alternateName);
            Assert.Equal($"Deleted command '{commandToDelete.Name}'", response);
        }

        [Fact]
        public async Task Command_Gets_Added_Properly()
        {
            var commandContainer = new Lazy<CommandContainer>(() => new CommandContainer(Enumerable.Empty<Command>()));
            var addCmd = new AddCommand(commandContainer);
            commandContainer.Value.Commands.Add(addCmd);

            var messageContent = $"{addCmd.Name} !test nah fam <nice>";

            var discordMessage = new DiscordMessage(
                messageContent,
                "",
                new User("", true),
                0);

            var commandParam = new CommandParameter<DiscordMessage>(messageContent.Split(), discordMessage);
            var response = await addCmd.Handle(commandParam);

            Assert.Contains(commandContainer.Value.Commands,
                x => x.Value.Name == "!test"
                    && x.Value.Description == "nice"
                    && x.Value.Response == "nah fam");

            Assert.Equal("Added command '!test'", response);
        }
    }
}