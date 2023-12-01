using calledudeBot.Chat.Info;
using calledudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public sealed class AddCommand : SpecialCommand<CommandParameter>
{
    private readonly Lazy<ICommandContainer> _commandContainer;

    public AddCommand(Lazy<ICommandContainer> commandContainer)
    {
        Name = "!addcmd";
        Description = "Adds a command to the command list";
        RequiresMod = true;
        AlternateName = ["!add", "!addcommand"];
        _commandContainer = commandContainer;
    }

    private string CreateCommand(CommandParameter param)
    {
        var commands = _commandContainer.Value.Commands;
        var foundCommand =
            commands.GetExistingCommand(param.PrefixedWords)
            ?? commands.GetExistingCommand(param.Words.First());

        if (foundCommand is not null)
            return $"Conflicting command name usage found in command '{foundCommand.Name}'";

        var result = Create(param);
        if (!result.Success)
        {
            return result.Error;
        }
        else
        {
            commands.Add(result.Value);
            _commandContainer.Value.SaveCommandsToFile();
            return $"Added command '{result.Value.Name}'";
        }
    }

    private static Result<Command> Create(CommandParameter commandParameter)
    {
        if (commandParameter.PrefixedWords.Exists(HasSpecialChars))
            return Result.Fail<Command>("Special characters in commands are not allowed.");

        var name = commandParameter.PrefixedWords[0];

        var alts = commandParameter
            .PrefixedWords
            .Skip(1)
            .Distinct();

        var alternateName = alts.Any()
            ? alts.ToList()
            : null;

        var description = string.Join(" ", commandParameter.EnclosedWords)
                            .Trim('<', '>');

        var response = string.Join(" ", commandParameter.Words);
        var command = new Command
        {
            Name = name,
            AlternateName = alternateName,
            Description = description,
            Response = response
        };

        return command;
    }

    private static bool HasSpecialChars(string str)
    {
        str = str[0] == CommandUtils.CommandPrefix ? str[1..] : str;
        return !str.All(char.IsLetterOrDigit);
    }

    protected override Task<string> HandleCommand(CommandParameter param)
    {
        string response;
        //has user entered a command to create? i.e. !addcmd !test someAnswer
        if (param.PrefixedWords.Count >= 1 && param.Words.Any())
        {
            response = CreateCommand(param);
        }
        else
        {
            response = "You ok there bud? Try again.";
        }

        return Task.FromResult(response);
    }
}
