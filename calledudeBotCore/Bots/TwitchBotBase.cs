using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public abstract class TwitchBotBase : IMessageBot<IrcMessage>
{
    private bool _disposedValue;

    public abstract string Name { get; }

    protected IIrcClient IrcClient { get; }
    protected string ChannelName { get; }
    protected string Broadcaster { get; }

    protected CancellationToken CancellationToken { get; private set; }

    protected TwitchBotBase(IIrcClient ircClient, ITwitchConfig config)
    {
        ChannelName = config.TwitchChannel!;
        Broadcaster = config.TwitchChannel![1..];

        IrcClient = ircClient;

        IrcClient.Server = "irc.chat.twitch.tv";
        IrcClient.SuccessCode = 366;
        IrcClient.Nick = config.TwitchBotUsername!;
        IrcClient.ChannelName = ChannelName;
        IrcClient.Token = config.TwitchToken!;

        IrcClient.Failures = new HashSet<string>
        {
            ":tmi.twitch.tv NOTICE * :Improperly formatted auth",
            ":tmi.twitch.tv NOTICE * :Login authentication failed",
        };
    }

    public async Task SendMessageAsync(IrcMessage message)
        => await IrcClient.SendMessage(message);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;

        _ = Task.Run(async () =>
        {
            await IrcClient.Setup();
            await IrcClient.Start(cancellationToken);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await IrcClient.Logout();

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            IrcClient.Dispose();
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
