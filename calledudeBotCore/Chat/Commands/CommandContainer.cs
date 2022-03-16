using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace calledudeBot.Chat.Commands;

public interface ICommandContainer
{
	IDictionary<string, Command> Commands { get; }

	void SaveCommandsToFile();
}

public class CommandContainer : ICommandContainer //Implement IDictionary<string, Command>?
{
	public const string COMMANDFILE = "commands.json";

	public IDictionary<string, Command> Commands { get; } = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

	public CommandContainer(IEnumerable<Command> commands)
	{
		foreach (var command in commands)
		{
			Commands.Add(command);
		}
	}

	public void SaveCommandsToFile()
	{
		var filteredCommands = Commands.Values
			.Where(x => x.GetType() == typeof(Command))
			.Distinct();

		var commands =
			JsonConvert.SerializeObject(
				filteredCommands,
				Formatting.Indented,
				new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.Ignore
				});

		File.WriteAllText(COMMANDFILE, commands);
	}
}
