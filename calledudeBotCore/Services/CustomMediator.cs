using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public class CustomMediator : Mediator
{
	public CustomMediator(IServiceProvider serviceProvider) : base(serviceProvider) { }

	protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
		=> await Task.WhenAll(handlerExecutors.Select(handler => handler.HandlerCallback(notification, cancellationToken)));
}
