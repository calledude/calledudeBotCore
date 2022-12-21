using MediatR;

namespace calledudeBot.Chat;

public interface IMessage : INotification
{
	string Content { get; }
	User? Sender { get; }
	string? Channel { get; }
}

public abstract record Message : IMessage
{
	public required string Content { get; init; }
	public User? Sender { get; init; }
	public string? Channel { get; init; }
}
