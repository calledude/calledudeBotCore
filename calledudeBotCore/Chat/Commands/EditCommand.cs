using calledudeBot.Chat.Info;
using calledudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public class EditCommand : SpecialCommand<CommandParameter>
{
	private readonly Lazy<ICommandContainer> _commandContainer;

	public EditCommand(Lazy<ICommandContainer> commandContainer)
	{
		Name = "!editcmd";
		Description = "Edits a command that exists";
		RequiresMod = true;
		AlternateName = new List<string> { "!edit", "!editcommand" };
		_commandContainer = commandContainer;
	}

	protected override Task<string> HandleCommand(CommandParameter param)
	{
		string response;
		//has user entered a command to edit? i.e. !editcmd !test description what the fuck
		if (param.PrefixedWords.Count >= 1 && param.Words.Any())
		{
			response = Edit(param);
		}
		else
		{
			response = "You ok there bud? Try again.";
		}

		return Task.FromResult(response);
	}

	private string Edit(CommandParameter param)
	{
		var commands = _commandContainer.Value.Commands;
		var foundCommand = commands.GetExistingCommand(param.PrefixedWords[0]);

		if (foundCommand is SpecialCommand || foundCommand is SpecialCommand<CommandParameter>)
		{
			return "You can't edit a special command.";
		}
		else if (foundCommand is not null)
		{
			var response = param.Words.First().ToLower() switch
			{
				"name" => EditName(foundCommand, param.PrefixedWords),
				"description" or "desc" or "descr" => EditDescription(foundCommand, param.Words.Skip(1)),
				"response" or "resp" => EditResponse(foundCommand, param.Words.Skip(1)),
				"alternate" or "alt" => EditAlternateNames(foundCommand, param.Words.Skip(1).First(), param.PrefixedWords),
				_ => "No such sub command exists. Valid sub commands are 'name|description|response|alternate'"
			};

			if (response.Success)
			{
				_commandContainer.Value.SaveCommandsToFile();
				return response.Value;
			}

			return response.Error;
		}
		else
		{
			return "No such command to edit exists.";
		}
	}

	private Result<string> EditName(Command command, IEnumerable<string> prefixedWords)
	{
		var previousName = command.Name;
		command.Name = prefixedWords.Single();

		if (command.Name == previousName)
			return Result.Fail<string>("Nothing was changed.");

		_commandContainer.Value.Commands.Remove(previousName!);
		_commandContainer.Value.Commands.Add(command.Name, command);
		return $"Changed name of '{previousName}' to '{command.Name}'";
	}

	private static Result<string> EditDescription(Command command, IEnumerable<string> words)
	{
		var oldDescription = command.Description;
		command.Description = string.Join(" ", words.Skip(1));

		if (oldDescription == command.Description)
			return Result.Fail<string>("Nothing was changed");

		return $"Changed description of '{command.Name}'.";
	}

	private static Result<string> EditResponse(Command command, IEnumerable<string> words)
	{
		var oldResponse = command.Response;
		command.Response = string.Join(" ", words.Skip(1));

		if (oldResponse == command.Response)
			return Result.Fail<string>("Nothing was changed");

		return $"Changed response of '{command.Name}'.";
	}

	private Result<string> EditAlternateNames(Command command, string mode, IEnumerable<string> prefixedWords)
	{
		command.AlternateName ??= new List<string>();

		var oldAlternateNames = new HashSet<string>(command.AlternateName);

		string? response = null;
		if (mode == "add")
		{
			var addedAlternates = new List<string>();
			foreach (var alternateName in prefixedWords)
			{
				if (command.AlternateName.Contains(alternateName))
					continue;

				addedAlternates.Add(alternateName);
				command.AlternateName.Add(alternateName);
				_commandContainer.Value.Commands.Add(alternateName, command);
			}

			response = $"Added alternate names for '{command.Name}' - {string.Join(" » ", addedAlternates)}";
		}
		else if (mode == "remove")
		{
			var removedAlternates = new List<string>();
			foreach (var alternateName in prefixedWords)
			{
				if (!command.AlternateName.Contains(alternateName))
					continue;

				removedAlternates.Add(alternateName);
				command.AlternateName.Remove(alternateName);
				_commandContainer.Value.Commands.Remove(alternateName);
			}

			response = $"Removed alternate names for '{command.Name}' - {string.Join(" » ", removedAlternates)}";
		}
		else if (mode == "clear")
		{
			foreach (var alternateName in command.AlternateName)
			{
				_commandContainer.Value.Commands.Remove(alternateName);
			}

			command.AlternateName.Clear();

			response = $"Cleared all alternative names for {command.Name}";
		}

		if (oldAlternateNames.SetEquals(command.AlternateName))
		{
			return Result.Fail<string>("Nothing changed");
		}

		return response ?? Result.Fail<string>("Unknown subcommand.");
		//return "Unknown subcommand.";
	}
}
