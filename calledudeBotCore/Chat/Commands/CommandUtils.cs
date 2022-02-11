using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace calledudeBot.Chat.Commands;

public static class CommandUtils
{
    internal const char PREFIX = '!';
    internal static string CommandFile { get; } = "commands.json";

    public static bool IsCommand(string message)
        => message[0] == PREFIX && message.Length > 1;

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

    internal static void SaveCommandsToFile(this IDictionary<string, Command> commandDictionary)
    {
        var filteredCommands = commandDictionary.Values
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

        File.WriteAllText(CommandFile, commands);
    }

    internal static string AddPrefix(this string str)
        => str[0] == PREFIX ? str : $"{PREFIX}{str}";

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
