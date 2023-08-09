using System.Collections.Generic;

namespace calledudeBot.Chat.Commands;

public class Command
{
	public string? Response { get; set; }
	public virtual string? Name { get; set; }
	public string? Description { get; set; }
	public virtual bool RequiresMod { get; protected set; }
	public List<string>? AlternateName { get; set; }
}
