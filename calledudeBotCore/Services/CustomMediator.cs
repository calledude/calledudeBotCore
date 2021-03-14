using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services
{
    public class CustomMediator : Mediator
    {
        public CustomMediator(ServiceFactory serviceFactory) : base(serviceFactory) { }

        protected override async Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification, CancellationToken cancellationToken)
            => await Task.WhenAll(allHandlers.Select(handler => handler(notification, cancellationToken)));
    }
}
