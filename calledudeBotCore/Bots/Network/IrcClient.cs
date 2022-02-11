using calledudeBot.Chat;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Bots.Network;

public interface IIrcClient : IDisposable
{
    string? ChannelName { get; set; }
    HashSet<string>? Failures { get; set; }
    string? Nick { get; set; }
    string? Server { get; set; }
    int SuccessCode { get; set; }
    string? Token { get; set; }
    List<string>? MessageFilters { get; set; }

    event Func<string, string, Task>? MessageReceived;
    event Func<Task>? Ready;
    event Func<string, Task>? UnhandledMessage;

    Task Logout();
    Task SendMessage(IrcMessage message);
    Task Setup();
    Task Start();
    Task WriteLine(string message);
}

public sealed class IrcClient : IIrcClient
{
    public string? Nick { get; set; }
    public string? ChannelName { get; set; }
    public string? Token { get; set; }
    public string? Server
    {
        get => _tcpClient?.Server;
        set => _tcpClient.Server = value;
    }

    public int SuccessCode { get; set; }
    public HashSet<string>? Failures { get; set; }
    public List<string>? MessageFilters { get; set; }

    public event Func<Task>? Ready;
    public event Func<string, string, Task>? MessageReceived;
    public event Func<string, Task>? UnhandledMessage;

    private readonly ILogger<IrcClient> _logger;
    private readonly ITcpClient _tcpClient;

    private static readonly AsyncMonitor _lock = new();

    public IrcClient(ILogger<IrcClient> logger, ITcpClient tcpClient)
    {
        _logger = logger;
        _tcpClient = tcpClient;
        _tcpClient.Port = 6667;
    }

    public async Task Setup() => await _tcpClient.Setup();

    public async Task Start()
    {
        try
        {
            await TryLogin();
            await Listen();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured.");
            await _tcpClient.Reconnect(); //Since basically any exception will break the fuck out of the bot, reconnect
            await Start();
        }
    }

    private async Task TryLogin(int tries = 1)
    {
        try
        {
            var waitTask = Task.Delay(15000);
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

            _logger.LogError(e, "Login failed, trying again. Attempts: {tries}", tries);
            await Task.Delay(1000 * tries);
            await TryLogin(++tries);
        }
    }

    private async Task SendPong(string ping)
    {
        await WriteLine(ping.Replace("PING", "PONG"));
        _logger.LogInformation("Heartbeat sent.");
    }

    public async Task SendMessage(IrcMessage message) => await WriteLine($"PRIVMSG {ChannelName} :{message.Content}");

    public Task Logout()
    {
        _tcpClient.Close();
        return Task.CompletedTask;
    }

    private bool IsFailure(string buffer) => Failures?.Contains(buffer) ?? false;

    private bool ShouldFilterMessageFromLogging(string? buffer)
    {
        if (buffer is null)
            return false;

        if (MessageFilters is null)
            return false;

        return MessageFilters.Any(x => buffer.Contains(x));
    }

    private async IAsyncEnumerable<(string?, string[]?)> MessageLoop(Func<bool> loopCondition)
    {
        while (loopCondition())
        {
            if (_tcpClient.InputStream is null)
                continue;

            var buffer = await _tcpClient.InputStream.ReadLineAsync();

            if (!ShouldFilterMessageFromLogging(buffer))
            {
                _logger.LogTrace("[{Server}]: {buffer}", Server, buffer);
            }

            yield return (buffer, buffer?.Split());
        }
    }

    private async Task Login()
    {
        using var lockObject = await _lock.EnterAsync();

        await WriteLine($"PASS {Token}\r\nNICK {Nick}\r\n");
        var resultCode = 0;

        await foreach ((var buffer, var splitBuffer) in MessageLoop(() => resultCode != SuccessCode))
        {
            _ = int.TryParse(splitBuffer?[1], out resultCode);
            if (buffer is null || IsFailure(buffer))
            {
                throw new InvalidOrWrongTokenException();
            }
            else if (resultCode == 001)
            {
                if (ChannelName is not null)
                {
                    await WriteLine($"JOIN {ChannelName}");
                }

                if (Ready is not null)
                {
                    await Ready();
                }

                _logger.LogInformation("Logged in as {userName}.", Nick);
            }
        }
    }

    private async Task Listen()
    {
        _logger.LogDebug("Starting message loop listener");
        await foreach ((var buffer, var splitBuffer) in MessageLoop(() => true))
        {
            if (buffer is null || splitBuffer is null)
                continue;

            if (splitBuffer[0].Equals("PING"))
            {
                await SendPong(buffer);
            }
            else if (splitBuffer[1].Equals("PRIVMSG"))
            {
                if (MessageReceived is null)
                    continue;

                var parsedMessage = IrcMessage.ParseMessage(splitBuffer);
                var parsedUser = IrcMessage.ParseUser(buffer);

                _ = MessageReceived(parsedMessage, parsedUser);
            }
            else
            {
                if (UnhandledMessage is null)
                    continue;

                _ = UnhandledMessage(buffer);
            }
        }
    }

    public async Task WriteLine(string message)
    {
        if (_tcpClient.OutputStream is null)
            return;

        await _tcpClient.OutputStream.WriteLineAsync(message);
    }

    public void Dispose() => _tcpClient.Dispose();
}