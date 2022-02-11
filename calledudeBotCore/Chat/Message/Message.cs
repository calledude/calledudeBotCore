using MediatR;

namespace calledudeBot.Chat;

public interface IMessage : INotification
{
    string Content { get; }
    User Sender { get; }
    string Channel { get; }
}

public interface IMessage<out T> : IMessage
{
    T CloneWithMessage(string message);
}

public abstract class Message<T> : IMessage<T>
{
    public string Content { get; }
    public User Sender { get; }
    public string Channel { get; }

    protected Message(string message, string channel, User sender)
    {
        Content = message;
        Sender = sender;
        Channel = channel;
    }

    public abstract T CloneWithMessage(string message);
}
