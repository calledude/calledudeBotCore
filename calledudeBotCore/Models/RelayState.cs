using System;

namespace calledudeBot.Models;

public interface IRelayState
{
	DateTime LastMessage { get; set; }
}

public class RelayState : IRelayState
{
	public DateTime LastMessage { get; set; } = DateTime.Now;
}
