namespace calledudeBot.Chat
{
    //Rebuild to pipe through commandhandler (commandpipeline)?
    //public class MessageHandler<T> : INotificationHandler<T> where T : IMessage<T>
    //{
    //    private readonly ILogger _logger;
    //    private readonly IMessageBot<T> _bot;
    //    private readonly IMessageDispatcher _dispatcher;

    //    public MessageHandler(ILogger<MessageHandler<T>> logger, IMessageBot<T> bot, IMessageDispatcher dispatcher)
    //    {
    //        _logger = logger;
    //        _bot = bot;
    //        _dispatcher = dispatcher;
    //    }

    //    public async Task Handle(T notification, CancellationToken cancellationToken)
    //    {
    //        _logger.LogInformation("Handling message: {0} from {1} in {2}", notification.Content, notification.Sender!.Name, notification.Channel);

    //        //Do this verification in CommandPipeline instead??
    //        var contentSplit = notification.Content.Split();
    //        if (CommandUtils.IsCommand(contentSplit[0]))
    //        {
    //            return;
    //        }

    //        await _dispatcher.PublishAsync(new RelayNotification<T>(_bot, notification));
    //    }
    //}
}