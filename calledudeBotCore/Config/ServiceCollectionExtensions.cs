using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Database;
using calledudeBot.Database.UserActivity;
using calledudeBot.Database.UserSession;
using calledudeBot.Models;
using calledudeBot.Services;
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

namespace calledudeBot.Config
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
            => services
                .AddMediatR(x => x.Using<CustomMediator>(), Assembly.GetExecutingAssembly())
                .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.GuildPresences | GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildMessages
                }))
                .AddSingleton<IMessageDispatcher, MessageDispatcher>()
                .AddSingleton<RelayHandler>()
                .AddTransient<IStreamMonitor, StreamMonitor>()
                .AddSingleton<UserActivityService>()
                .AddTransient<IIrcClient, IrcClient>();

        public static IServiceCollection AddBots(this IServiceCollection services)
            => services
                .AddSingleton<ITwitchUser, TwitchUser>()
                .AddSingleton<IMessageBot<DiscordMessage>, DiscordBot>()
                .AddSingleton<SteamBot>()
                .AddSingleton<IMessageBot<IrcMessage>, TwitchBot>();

        public static IServiceCollection AddTwitchConfigs(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetFileLoadExceptionHandler(x =>
                {
                    if (x.Exception is FileNotFoundException)
                    {
                        var cfg = JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented);

                        File.WriteAllText(x.Provider.Source.Path, cfg);
                        var logger = Log.Logger.ForContext(typeof(calledudeBot));
                        logger.Fatal("No config file detected. Created one for you with default values, please fill it in.");
                        logger.Information("Press any key to exit..");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                })
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .Build();

            services.Configure<BotConfig>(configuration);

            var twitchConfigs = configuration.GetSection("TwitchBotConfigs").Get<TwitchBotConfig[]>();

            services.AddSingleton<ITwitchBotConfig>(twitchConfigs.First(x => !x.IsUser));
            services.AddSingleton<ITwitchUserConfig>(twitchConfigs.First(x => x.IsUser));

            return services;
        }

        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            var commands = JsonConvert.DeserializeObject<List<Command>>(File.ReadAllText(CommandUtils.CommandFile)) ?? new List<Command>();

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

            services.AddSingleton(x => new CommandContainer(x.GetServices<Command>()));
            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services)
        {
            return services
                .AddDbContext<DatabaseContext>(ServiceLifetime.Transient)
                .AddTransient<IUserActivityRepository, UserActivityRepository>()
                .AddTransient<IUserSessionRepository, UserSessionRepository>();
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
            => services.AddTransient(typeof(Lazy<>), typeof(LazilyResolved<>));
    }
}
