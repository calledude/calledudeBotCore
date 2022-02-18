using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IMessageDispatcher
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}

public class MessageDispatcher : IMessageDispatcher
{
    private readonly ILogger<MessageDispatcher> _logger;
    private readonly IMediator _mediator;

    public MessageDispatcher(ILogger<MessageDispatcher> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var notificationType = notification.GetType().Name;
        _logger.LogInformation("Beginning to publish a {notificationType} message", notificationType);

        try
        {
            await _mediator.Publish(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception was thrown in the MediatR adapter");
        }

        _logger.LogInformation("Finished invoking {notificationType} handlers", notificationType);
    }
}
