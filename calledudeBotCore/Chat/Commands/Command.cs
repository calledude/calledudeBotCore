using calledudeBot.Chat.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace calledudeBot.Chat.Commands;

public class Command
{
    public string? Response { get; set; }
    public virtual string? Name { get; set; }
    public string? Description { get; set; }
    public virtual bool RequiresMod { get; protected set; }
    public List<string>? AlternateName { get; set; }

    public Command(CommandParameter cmdParam)
    {
        if (cmdParam.PrefixedWords.Any(HasSpecialChars))
            throw new ArgumentException("Special characters in commands are not allowed.");

        Name = cmdParam.PrefixedWords[0];

        var alts = cmdParam
            .PrefixedWords
            .Skip(1)
            .Distinct();

        AlternateName = alts.Any()
            ? alts.ToList()
            : AlternateName;

        Description = string.Join(" ", cmdParam.EnclosedWords)
                            .Trim('<', '>');

        Response = string.Join(" ", cmdParam.Words);
    }

    public Command()
    {
    }

    private static bool HasSpecialChars(string str)
    {
        str = str[0] == CommandUtils.PREFIX ? str[1..] : str;
        return !str.All(char.IsLetterOrDigit);
    }
}
