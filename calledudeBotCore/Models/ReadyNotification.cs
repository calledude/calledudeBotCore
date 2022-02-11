using MediatR;
using Microsoft.Extensions.Hosting;

namespace calledudeBot.Models;

public class ReadyNotification : INotification
{
    public IHostedService Bot { get; }

    public ReadyNotification(IHostedService bot)
    {
        Bot = bot;
    }
}
