﻿using calledudeBot.Chat.Info;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public sealed class DeleteCommand : SpecialCommand<CommandParameter>
{
	private readonly Lazy<CommandContainer> _commandContainer;

	public DeleteCommand(Lazy<CommandContainer> commandContainer)
	{
		Name = "!delcmd";
		Description = "Deletes a command from the command list";
		RequiresMod = true;
		_commandContainer = commandContainer;
	}

	protected override Task<string> HandleCommand(CommandParameter param)
	{
		var response = "You ok there bud? Try again.";

		var cmdToDel = param.PrefixedWords.FirstOrDefault()
			?? param.Words.FirstOrDefault()?.AddPrefix();

		if (_commandContainer.Value.Commands.GetExistingCommand(cmdToDel) is Command c)
		{
			response = RemoveCommand(c, cmdToDel);
		}

		return Task.FromResult(response);
	}

	private string RemoveCommand(Command cmd, string? altName = null)
	{
		if (cmd is SpecialCommand)
			return "You can't remove a special command.";

		string response;

		if (altName != cmd.Name && altName is not null)
		{
			cmd.AlternateName!.Remove(altName);
			_commandContainer.Value.Commands.Remove(altName);
			response = $"Deleted alternative command '{altName}'";
		}
		else
		{
			_commandContainer.Value.Commands.Remove(cmd.Name!);

			if (cmd.AlternateName is not null)
			{
				foreach (var alt in cmd.AlternateName)
				{
					_commandContainer.Value.Commands.Remove(alt);
				}
			}

			response = $"Deleted command '{altName}'";
		}

		_commandContainer.Value.SaveCommandsToFile();

		return response;
	}
}
