using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class MessageDispatcherTests
{
    private readonly Logger<MessageDispatcher> _logger;
    private readonly INotification _notification;
    private readonly INotificationHandler<INotification> _throwingHandler;
    private readonly INotificationHandler<INotification> _normalHandler;

    public MessageDispatcherTests()
    {
        _logger = LoggerObjectMother.NullLoggerFor<MessageDispatcher>();
        _notification = Substitute.For<INotification>();

        _throwingHandler = Substitute.For<INotificationHandler<INotification>>();

        _throwingHandler
            .Handle(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        _normalHandler = Substitute.For<INotificationHandler<INotification>>();
    }

    [Fact]
    public async Task Publish_Throws_AllActionsAreExecuted()
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock
            .GetService(Arg.Any<Type>()) // This is stupid
            .Returns(new[]
            {
                _throwingHandler,
                _normalHandler
            });

        var mediator = new CustomMediator(serviceProviderMock);

        var dispatcher = new MessageDispatcher(_logger, mediator);

        await dispatcher.PublishAsync(_notification);

        await _throwingHandler.Received(1).Handle(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        await _normalHandler.Received(1).Handle(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_NeverThrows()
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock
            .GetService(Arg.Any<Type>())
            .Returns(new[]
            {
                _throwingHandler,
            });

        var mediator = new CustomMediator(serviceProviderMock);

        var dispatcher = new MessageDispatcher(_logger, mediator);

        var publishTask = dispatcher.PublishAsync(_notification);
        await publishTask;

        Assert.False(publishTask.IsFaulted);
    }

    [Fact]
    public async Task Publish_WithNormalHandler_Works()
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock
            .GetService(Arg.Any<Type>())
            .Returns(new[]
            {
                _normalHandler,
            });

        var mediator = new CustomMediator(serviceProviderMock);

        var dispatcher = new MessageDispatcher(_logger, mediator);

        var publishTask = dispatcher.PublishAsync(_notification);
        await publishTask;

        Assert.False(publishTask.IsFaulted);
    }
}
