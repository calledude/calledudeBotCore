using System;

namespace calledudeBot.Models;

public interface IStreamingState
{
	Guid SessionId { get; set; }
	bool IsStreaming { get; set; }
	DateTime StreamStarted { get; set; }
}

public class StreamingState : IStreamingState
{
	public Guid SessionId { get; set; }
	public bool IsStreaming { get; set; }
	public DateTime StreamStarted { get; set; }
}
