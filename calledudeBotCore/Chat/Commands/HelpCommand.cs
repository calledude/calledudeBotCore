using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat
{
    public sealed class HelpCommand : SpecialCommand<CommandParameter>
    {
        private readonly Lazy<CommandContainer> _commandContainer;

        public HelpCommand(Lazy<CommandContainer> commandContainer)
        {
            Name = "!help";
            AlternateName = new List<string> { "!commands", "!cmds" };
            Description = "Lists all available commands or helps you with a specific one.";
            RequiresMod = false;
            _commandContainer = commandContainer;
        }

        protected override async Task<string> HandleCommand(CommandParameter param)
        {
            const string errorResponse = "You ok there bud? Try again.";
            var cmdToHelp = param.PrefixedWords.FirstOrDefault() ?? param.Words.FirstOrDefault();

            if (cmdToHelp == null)
            {
                var allowed = await param.SenderIsMod();

                var availableCommands = _commandContainer.Value.Commands.Values
                                        .Distinct()
                                        .Where(x => !x.RequiresMod || allowed)
                                        .Select(x => x.Name);

                var commands = string.Join(" » ", availableCommands);

                return $"These are the commands you can use: {commands}";
            }
            else if (_commandContainer.Value.Commands.GetExistingCommand(cmdToHelp) is Command c) //"!help <command>"
            {
                if (c.RequiresMod && !await param.SenderIsMod())
                    return errorResponse;

                var cmds = c.Name;
                if (c.AlternateName?.Count > 0)
                {
                    var alts = string.Join("/", c.AlternateName);
                    cmds += $"/{alts}";
                }

                var responseDescription = string.IsNullOrEmpty(c.Description)
                    ? "has no description."
                    : $"has the description '{c.Description}'";

                return $"Command '{cmds}' {responseDescription}";
            }

            return errorResponse;
        }
    }
}