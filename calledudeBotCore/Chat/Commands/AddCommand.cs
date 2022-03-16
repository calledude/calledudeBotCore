using calledudeBot.Chat.Info;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

//TODO: Rewrite this, this is utter garbage
// 1. Can't edit command name
// 2. Need to expand functionality around alternate name editing
//	2a. Editing the alternate name of something is not possible, i.e. !oldAlt -> !newAlt, since Count needs to be non-equal
//	2b. The response is lazy
//	2c. Removing a single alternate name is annoying
// 3. Should probably split this out into either sub-commands, i.e. !addcommand edit <name|description|response|etc> or entirely new commands, !editcommand
// 4. Multiple changes response is perhaps a bit lazy
// 5. Say I just want to edit the description of something, I have to provide the command as-is in order to not overwrite anything else
// 6. Checking 'changes' twice is ugly
public sealed class AddCommand : SpecialCommand<CommandParameter>
{
	private readonly Lazy<ICommandContainer> _commandContainer;

	public AddCommand(Lazy<ICommandContainer> commandContainer)
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

				return $"Conflicting command name usage found in command '{foundCommand.Name}'";
			}
			else
			{
				commands.Add(newCommand);
				_commandContainer.Value.SaveCommandsToFile();
				return $"Added command '{newCommand.Name}'";
			}
		}
		catch (ArgumentException e)
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

	private string EditCommand(Command foundCommand, Command newCommand)
	{
		if (foundCommand is SpecialCommand || foundCommand is SpecialCommand<CommandParameter>)
			return "You can't change a special command.";

		var changes = 0;
		var response = $"Command '{newCommand.Name}' already exists.";

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

		if (changes >= 1)
		{
			_commandContainer.Value.SaveCommandsToFile();
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
			var newAlternates = foundCommand.AlternateName ?? Enumerable.Empty<string>();
			foundCommand.AlternateName = newAlternates
				.Concat(newCommand.AlternateName)
				.Distinct()
				.ToList();

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
