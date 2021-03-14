using calledudeBot.Chat;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace calledudeBot.Bots
{
    public interface IIrcClient
    {
        string? ChannelName { get; set; }
        HashSet<string>? Failures { get; set; }
        string? Nick { get; set; }
        int Port { get; set; }
        string? Server { get; set; }
        int SuccessCode { get; set; }
        string? Token { get; set; }

        event Func<string, string, Task>? MessageReceived;
        event Func<Task>? Ready;
        event Func<string, Task>? UnhandledMessage;

        void Dispose();
        Task Listen();
        Task Login();
        Task Logout();
        Task Reconnect();
        Task SendMessage(IrcMessage message);
        Task SendPong(string ping);
        Task Setup();
        Task Start();
        Task WriteLine(string message);
    }

    public sealed class IrcClient : IDisposable, IIrcClient
    {
        public string? Nick { get; set; }
        public string? ChannelName { get; set; }
        public string? Token { get; set; }
        public string? Server { get; set; }
        public int Port { get; set; } = 6667;
        public int SuccessCode { get; set; }
        public HashSet<string>? Failures { get; set; }

        public event Func<Task>? Ready;
        public event Func<string, string, Task>? MessageReceived;
        public event Func<string, Task>? UnhandledMessage;

        private readonly ILogger<IrcClient> _logger;
        private TcpClient? _sock;
        private StreamWriter? _output;
        private StreamReader? _input;

        private static readonly AsyncMonitor _lock = new();

        public IrcClient(ILogger<IrcClient> logger)
        {
            _logger = logger;
        }

        public async Task Setup()
        {
            _sock = new TcpClient();
            await _sock.ConnectAsync(Server!, Port);
            _output = new StreamWriter(_sock.GetStream())
            {
                AutoFlush = true
            };
            _input = new StreamReader(_sock.GetStream());
        }

        public async Task Start()
        {
            await TryLogin();

            try
            {
                await Listen();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured.");
                await Reconnect(); //Since basically any exception will break the fuck out of the bot, reconnect
            }
        }

        private async Task TryLogin(int tries = 1)
        {
            try
            {
                var waitTask = Task.Delay(5000);
                var loginTask = Login();

                var completedTask = await Task.WhenAny(waitTask, loginTask);

                if (loginTask.IsFaulted)
                {
                    _logger.LogError(loginTask.Exception, "Login failed. Are you sure your credentials are correct?");
                    throw loginTask.Exception!.InnerException!;
                }
                else if (completedTask != loginTask)
                {
                    var timeoutException = new TimeoutException();
                    _logger.LogError(timeoutException, "Login timed out. Are you sure your credentials are correct?");
                    throw timeoutException;
                }
            }
            catch (Exception e) when (tries <= 3)
            {
                //TODO: Potentially run Setup()? Not sure if needed

                _logger.LogError(e, "Login failed, trying again. Attempts: {0}", tries);
                await Task.Delay(1000 * tries);
                await TryLogin(++tries);
            }
        }

        public async Task SendPong(string ping)
        {
            await WriteLine(ping.Replace("PING", "PONG"));
            _logger.LogInformation("Heartbeat sent.");
        }

        public async Task SendMessage(IrcMessage message)
            => await WriteLine($"PRIVMSG {ChannelName} :{message.Content}");

        public async Task Reconnect()
        {
            _logger.LogWarning("Disconnected. Re-establishing connection..");
            Dispose();

            for (var retryMultiplier = 1; !_sock!.Connected; retryMultiplier *= 2)
            {
                try
                {
                    Dispose();
                    await Setup();

                    var nextRetryInMs = 5000 * retryMultiplier;
                    _logger.LogInformation("Retrying in {0} seconds. Retries made: {1}", nextRetryInMs / 1000, (retryMultiplier / 2) + 1);
                    await Task.Delay(nextRetryInMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An exception occured when trying to reconnect");
                }
            }
            await Start();
        }

        public Task Logout()
        {
            _sock!.Close();
            return Task.CompletedTask;
        }

        private bool IsFailure(string buffer)
            => Failures?.Contains(buffer) ?? false;

        private async Task MessageLoop(Func<bool> loopCondition, Func<string?, string[]?, Task> callback)
        {
            while (loopCondition())
            {
                if (_input == null)
                    continue;

                var buffer = await _input.ReadLineAsync();

                _logger.LogTrace(buffer);

                await callback(buffer, buffer?.Split());
            }
        }

        public async Task Login()
        {
            using var lockObject = await _lock.EnterAsync();

            await WriteLine($"PASS {Token}\r\nNICK {Nick}\r\n");
            var resultCode = 0;

            await MessageLoop(() => resultCode != SuccessCode, async (buffer, splitBuffer) =>
            {
                _ = int.TryParse(splitBuffer?[1], out resultCode);
                if (buffer == null || IsFailure(buffer))
                {
                    throw new InvalidOrWrongTokenException();
                }
                else if (resultCode == 001)
                {
                    if (ChannelName != null)
                        await WriteLine($"JOIN {ChannelName}");

                    if (Ready != null)
                        await Ready();

                    _logger.LogInformation("Logged in as {0}.", Nick);
                }
            });
        }

        public async Task Listen()
        {
            _logger.LogDebug("Starting message loop listener");
            await MessageLoop(() => true, async (buffer, splitBuffer) =>
            {
                if (buffer == null || splitBuffer == null)
                    return;

                if (splitBuffer[0].Equals("PING"))
                {
                    await SendPong(buffer);
                }
                else if (splitBuffer[1].Equals("PRIVMSG"))
                {
                    if (MessageReceived == null)
                        return;

                    var parsedMessage = IrcMessage.ParseMessage(splitBuffer);
                    var parsedUser = IrcMessage.ParseUser(buffer);
                    await MessageReceived(parsedMessage, parsedUser);
                }
                else
                {
                    if (UnhandledMessage == null)
                        return;

                    await UnhandledMessage(buffer);
                }
            });
        }

        public async Task WriteLine(string message)
        {
            if (_output == null)
                return;

            await _output.WriteLineAsync(message);
        }

        public void Dispose()
        {
            _sock?.Dispose();
            _input?.Dispose();
            _output?.Dispose();
        }
    }
}
