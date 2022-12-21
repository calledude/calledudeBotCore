using calledudeBot.Bots;
using calledudeBot.Bots.Network;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Database;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using calledudeBot.Models;
using calledudeBot.Services;
using calledudeBot.Utilities;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace calledudeBot.Config;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddServices(this IServiceCollection services)
		=> services
			.AddMediatR(x => x.Using<CustomMediator>(), Assembly.GetExecutingAssembly())
			.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
			{
				GatewayIntents = GatewayIntents.GuildPresences | GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildMessages
			}))
			.AddHttpClient()
			.AddSingleton<IMessageDispatcher, MessageDispatcher>()
			.AddSingleton<IRelayState, RelayState>()
			.AddSingleton<IStreamingState, StreamingState>()
			.AddSingleton<IUserActivityService, UserActivityService>()
			.AddTransient<IStreamMonitor, StreamMonitor>()
			.AddTransient<IDiscordSocketClient, DiscordSocketClientWrapper>()
			.AddTransient<IUserSessionService, UserSessionService>()
			.AddTransient<IIrcClient, IrcClient>()
			.AddTransient<IOsuUserService, OsuUserService>()
			.AddTransient<IHttpClientWrapper, HttpClientWrapper>()
			.AddTransient<IAsyncTimer, AsyncTimer>();

	public static IServiceCollection AddBots(this IServiceCollection services)
		=> services
			.AddTransient<ITcpClient, TcpClientAdapter>()
			.AddSingleton<ITwitchUser, TwitchUser>()
			.AddSingleton<IMessageBot<DiscordMessage>, DiscordBot>()
			.AddSingleton<ISteamBot, SteamBot>()
			.AddSingleton<IOsuBot, OsuBot>()
			.AddSingleton<ITwitchBot, TwitchBot>()
			.AddSingleton<IMessageBot<IrcMessage>>(x => x.GetRequiredService<ITwitchBot>());

	public static IServiceCollection AddTwitchConfigs(this IServiceCollection services)
	{
		var configuration = new ConfigurationBuilder()
			.SetFileLoadExceptionHandler(x =>
			{
				if (x.Exception is FileNotFoundException)
				{
					var cfg = JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented);

					File.WriteAllText(x.Provider.Source.Path!, cfg);
					Log.Logger.Fatal("No config file detected. Created one for you with default values, please fill it in.");
					Log.Logger.Information("Press any key to exit..");
					Console.ReadKey();
					Environment.Exit(0);
				}
			})
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("config.json", optional: false)
			.Build();

		services.Configure<BotConfig>(configuration);

		const string twitchBotConfigSection = "TwitchBotConfigs";
		var twitchConfigs = configuration.GetSection("TwitchBotConfigs").Get<TwitchBotConfig[]>()
			?? throw new InvalidOperationException($"Section {twitchBotConfigSection} does not exist in config file.");

		services.AddSingleton<ITwitchBotConfig>(twitchConfigs.First(x => !x.IsUser));
		services.AddSingleton<ITwitchUserConfig>(twitchConfigs.First(x => x.IsUser));

		return services;
	}

	public static IServiceCollection AddCommands(this IServiceCollection services)
	{
		if (!File.Exists(CommandContainer.COMMANDFILE))
		{
			File.WriteAllText(CommandContainer.COMMANDFILE, JsonConvert.SerializeObject(new List<Command>()));
		}

		var commands = JsonConvert.DeserializeObject<Command[]>(File.ReadAllText(CommandContainer.COMMANDFILE)) ?? Array.Empty<Command>();

		var specialCommands = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(x => x.IsSubclassOf(typeof(Command)) && !x.IsAbstract);

		foreach (var cmd in specialCommands)
		{
			services.AddSingleton(typeof(Command), cmd);
		}

		foreach (var cmd in commands)
		{
			services.AddSingleton(typeof(Command), cmd);
		}

		services.AddSingleton<ICommandContainer, CommandContainer>();
		return services;
	}

	public static IServiceCollection AddDatabase(this IServiceCollection services)
		=> services
			.AddDbContext<DatabaseContext>(ServiceLifetime.Transient)
			.AddTransient<IUserActivityRepository, UserActivityRepository>()
			.AddTransient<IUserSessionRepository, UserSessionRepository>();

	public static IServiceCollection AddLazyResolution(this IServiceCollection services)
		=> services.AddTransient(typeof(Lazy<>), typeof(LazilyResolved<>));
}
