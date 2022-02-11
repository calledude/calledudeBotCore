using calledudeBot.Chat.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public sealed class AddCommand : SpecialCommand<CommandParameter>
{
    private readonly Lazy<CommandContainer> _commandContainer;

    public AddCommand(Lazy<CommandContainer> commandContainer)
    {
        Name = "!addcmd";
        Description = "Adds a command to the command list";
        RequiresMod = true;
        _commandContainer = commandContainer;
    }

    private string CreateCommand(CommandParameter param)
    {
        try
        {
            var commands = _commandContainer.Value.Commands;
            var foundCommand =
                commands.GetExistingCommand(param.PrefixedWords)
                ?? commands.GetExistingCommand(param.Words.First());

            var newCommand = new Command(param);

            if (foundCommand is not null)
            {
                if (foundCommand.Name!.Equals(newCommand.Name))
                    return EditCommand(foundCommand, newCommand);

                return "One or more of the alternate commands already exists.";
            }
            else
            {
                commands.Add(newCommand);
                commands.SaveCommandsToFile();
                return $"Added command '{newCommand.Name}'";
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    protected override Task<string> HandleCommand(CommandParameter param)
    {
        string response;
        //has user entered a command to enter? i.e. !addcmd !test someAnswer
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

    private static string EditCommand(Command foundCommand, Command newCommand)
    {
        string response;
        if (foundCommand is SpecialCommand || foundCommand is SpecialCommand<CommandParameter>)
            return "You can't change a special command.";

        var changes = 0;
        response = $"Command '{newCommand.Name}' already exists.";

        if (newCommand.Response != foundCommand.Response)
        {
            response = EditCommandResponse(foundCommand, newCommand, ref changes);
        }

        if (newCommand.Description != foundCommand.Description)
        {
            response = EditCommandDescription(foundCommand, newCommand, ref changes);
        }

        if (newCommand.AlternateName?.Count != foundCommand.AlternateName?.Count)
        {
            response = EditCommandAlternateNames(foundCommand, newCommand, ref changes);
        }

        return changes > 1 ? $"Done. Several changes made to command '{newCommand.Name}'." : response;
    }

    private static string EditCommandAlternateNames(Command foundCommand, Command newCommand, ref int changes)
    {
        string response;
        if (newCommand.AlternateName == default)
        {
            foundCommand.AlternateName = newCommand.AlternateName;
            response = $"Removed all alternate commands for '{foundCommand.Name}'";
        }
        else
        {
            (foundCommand.AlternateName ??= new List<string>()).AddRange(newCommand.AlternateName);
            foundCommand.AlternateName = foundCommand.AlternateName.Distinct().ToList();
            response = $"Changed alternate command names for '{foundCommand.Name}'. It now has {foundCommand.AlternateName.Count} alternates.";
        }
        changes++;
        return response;
    }

    private static string EditCommandDescription(Command foundCommand, Command newCommand, ref int changes)
    {
        foundCommand.Description = newCommand.Description;
        changes++;
        return $"Changed description of '{foundCommand.Name}'.";
    }

    private static string EditCommandResponse(Command foundCommand, Command newCommand, ref int changes)
    {
        foundCommand.Response = newCommand.Response;
        changes++;
        return $"Changed response of '{foundCommand.Name}'.";
    }
}
