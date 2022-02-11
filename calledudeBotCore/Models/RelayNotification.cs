using calledudeBot.Bots;
using calledudeBot.Chat;
using MediatR;

namespace calledudeBot.Models;

public class RelayNotification<T> : INotification where T : IMessage<T>
{
    public T Message { get; }
    public IMessageBot<T> Bot { get; }

    public RelayNotification(IMessageBot<T> bot, T message)
    {
        Bot = bot;
        Message = message;
    }
}
