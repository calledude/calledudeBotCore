using calledudeBot.Chat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Bots
{
    public interface IMessageBot<T> : IHostedService, IDisposable where T : IMessage
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
        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task SendMessageAsync(T message)
        {
            _logger.LogInformation("Sending message: {0}", message.Content);
            await SendMessage(message);
        }
    }

    internal class InvalidOrWrongTokenException : Exception
    {
        public InvalidOrWrongTokenException()
        {
        }

        public InvalidOrWrongTokenException(string? message) : base(message)
        {
        }

        public InvalidOrWrongTokenException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
