using System;
using System.Collections.Generic;

namespace calledudeBot.Chat.Commands;

public class CommandContainer //Implement IDictionary<string, Command>?
{
    public IDictionary<string, Command> Commands { get; set; } = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

    public CommandContainer(IEnumerable<Command> commands)
    {
        foreach (var command in commands)
        {
            Commands.Add(command);
        }
    }
}
