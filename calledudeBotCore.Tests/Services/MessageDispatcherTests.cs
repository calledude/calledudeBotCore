using calledudeBot.Services;
using calledudeBotCore.Tests.ObjectMothers;
using MediatR;
using Microsoft.Extensions.Logging;
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
		_logger = LoggerObjectMother.NullLoggerFor<MessageDispatcher>();
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
		var serviceProviderMock = new Mock<IServiceProvider>();
		serviceProviderMock
			.Setup(x => x.GetService(It.IsAny<Type>())) // This is stupid
			.Returns(new[]
			{
				_throwingHandler.Object,
				_normalHandler.Object
			});

		var mediator = new CustomMediator(serviceProviderMock.Object);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		await dispatcher.PublishAsync(_notification);

		_throwingHandler.Verify(x => x.Handle(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
		_normalHandler.Verify(x => x.Handle(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_NeverThrows()
	{
		var serviceProviderMock = new Mock<IServiceProvider>();
		serviceProviderMock
			.Setup(x => x.GetService(It.IsAny<Type>()))
			.Returns(new[]
			{
				_throwingHandler.Object,
			});

		var mediator = new CustomMediator(serviceProviderMock.Object);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		var publishTask = dispatcher.PublishAsync(_notification);
		await publishTask;

		Assert.False(publishTask.IsFaulted);
	}

	[Fact]
	public async Task Publish_WithNormalHandler_Works()
	{
		var serviceProviderMock = new Mock<IServiceProvider>();
		serviceProviderMock
			.Setup(x => x.GetService(It.IsAny<Type>()))
			.Returns(new[]
			{
				_normalHandler.Object,
			});

		var mediator = new CustomMediator(serviceProviderMock.Object);

		var dispatcher = new MessageDispatcher(_logger, mediator);

		var publishTask = dispatcher.PublishAsync(_notification);
		await publishTask;

		Assert.False(publishTask.IsFaulted);
	}
}
