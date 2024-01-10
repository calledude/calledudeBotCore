using System;

namespace calledudeBot.Bots.Network;

public class InvalidOrWrongTokenException : Exception
{
	public InvalidOrWrongTokenException()
	{
	}

	public InvalidOrWrongTokenException(string? message) : base(message)
	{
	}

	public InvalidOrWrongTokenException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}