using System.Collections.Generic;

namespace calledudeBot.Chat.Commands;

public static class CommandUtils
{
	public const char CommandPrefix = '!';

	public static bool IsCommand(string message)
		=> message[0] == CommandPrefix && message.Length > 1;

	//Returns the Command object or null depending on if it exists or not.
	internal static Command? GetExistingCommand(this IDictionary<string, Command> commands, string? cmd)
	{
		if (string.IsNullOrWhiteSpace(cmd))
			return null;

		commands.TryGetValue(cmd.AddPrefix(), out var command);
		return command;
	}

	internal static Command? GetExistingCommand(this IDictionary<string, Command> commands, IEnumerable<string> prefixedWords)
	{
		foreach (var word in prefixedWords)
		{
			if (GetExistingCommand(commands, word) is Command c)
				return c;
		}

		return null;
	}

	internal static string AddPrefix(this string str)
		=> str[0] == CommandPrefix ? str : $"{CommandPrefix}{str}";

	public static void Add(this IDictionary<string, Command> commands, Command command)
	{
		commands.Add(command.Name!, command);

		if (command.AlternateName is null)
			return;

		foreach (var alt in command.AlternateName)
		{
			commands.Add(alt, command);
		}
	}
}
