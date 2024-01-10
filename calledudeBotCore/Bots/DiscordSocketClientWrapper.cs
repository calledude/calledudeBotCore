using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace calledudeBot.Bots;

public interface IDiscordSocketClient
{
	event Func<LogMessage, Task> Log;
	event Func<IMessage, Task> MessageReceived;
	event Func<Task> Ready;

	ISelfUser CurrentUser { get; }

	IMessageChannel? GetMessageChannel(ulong id);
	Task Login(string token);
	Task Logout();
	Task Start();
	Task Stop();
}

public class DiscordSocketClientWrapper : IDiscordSocketClient
{
	private readonly DiscordSocketClient _discordClient;

	public event Func<LogMessage, Task> Log
	{
		add => _discordClient.Log += value;
		remove => _discordClient.Log -= value;
	}

	public event Func<IMessage, Task> MessageReceived
	{
		add => _discordClient.MessageReceived += value;
		remove => _discordClient.MessageReceived -= value;
	}

	public event Func<Task> Ready
	{
		add => _discordClient.Ready += value;
		remove => _discordClient.Ready -= value;
	}

	public ISelfUser CurrentUser => _discordClient.CurrentUser;

	public DiscordSocketClientWrapper(DiscordSocketClient discordClient)
	{
		_discordClient = discordClient;
	}

	public async Task Login(string token)
		=> await _discordClient.LoginAsync(TokenType.Bot, token);

	public async Task Start()
		=> await _discordClient.StartAsync();

	public IMessageChannel? GetMessageChannel(ulong id)
		=> _discordClient.GetChannel(id) as IMessageChannel;

	public async Task Logout()
		=> await _discordClient.LogoutAsync();

	public async Task Stop()
		=> await _discordClient.StopAsync();
}