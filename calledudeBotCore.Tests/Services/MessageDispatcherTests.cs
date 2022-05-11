using calledudeBot.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace calledudeBotCore.Tests.Services;

public class MessageDispatcherTests
{
	private readonly Logger<MessageDispatcher> _logger;
	private readonly INotification _notification;
	private readonly Mock<INotificationHandler<INotification>> _throwingHandler;
	private readonly Mock<INotificationHandler<INotification>> _normalHandler;

	public MessageDispatcherTests()
	{
		_logger = new Logger<MessageDispatcher>(NullLoggerFactory.Instance);
		_notification = new Mock<INotification>().Object;

		_throwingHandler = new Mock<INotificationHandler<INotification>>();

		_throwingHandler
			.Setup(x => x.Handle(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException());

		_normalHandler = new Mock<INotificationHandler<INotification>>();
	}

	[Fact]
	public async Task Publish_Throws_AllActionsAreExecuted()
	{
		var notificationHandlers = new[]
		{
			_throwingHandler.Object,
			_normalHandler.Object
		};

		var mediator = new CustomMediator(_ => notificationHandlers);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		await dispatcher.PublishAsync(_notification);

		_throwingHandler.Verify(x => x.Handle(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
		_normalHandler.Verify(x => x.Handle(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_NeverThrows()
	{
		var notificationHandlers = new[]
		{
			_throwingHandler.Object
		};

		var mediator = new CustomMediator(_ => notificationHandlers);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		var publishTask = dispatcher.PublishAsync(_notification);
		await publishTask;

		Assert.False(publishTask.IsFaulted);
	}

	[Fact]
	public async Task Publish_WithNormalHandler_Works()
	{
		var notificationHandlers = new[]
		{
			_normalHandler.Object
		};

		var mediator = new CustomMediator(_ => notificationHandlers);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		var publishTask = dispatcher.PublishAsync(_notification);
		await publishTask;

		Assert.False(publishTask.IsFaulted);
	}
}
