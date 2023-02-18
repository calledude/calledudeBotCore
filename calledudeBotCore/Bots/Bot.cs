using calledudeBot.Chat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public interface IMessageBot<in T> : IHostedService where T : IMessage
{
	string Name { get; }
	Task SendMessageAsync(T message);
}

public abstract class Bot<T> : IMessageBot<T> where T : IMessage
{
	private readonly ILogger _logger;

	public abstract string Name { get; }

	protected Bot(ILogger logger)
	{
		_logger = logger;
	}

	public abstract Task StartAsync(CancellationToken cancellationToken);
	public abstract Task StopAsync(CancellationToken cancellationToken);
	protected abstract Task SendMessage(T message);

	public async Task SendMessageAsync(T message)
	{
		_logger.LogInformation("Sending message: '{content}'", message.Content);
		await SendMessage(message);
	}
}
