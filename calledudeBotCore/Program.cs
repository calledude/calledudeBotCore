using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Config;
using calledudeBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace calledudeBot
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "CalledudeBot is ugly :)")]
    public static class calledudeBot
    {
        public static async Task Main()
        {
            Console.Title = nameof(calledudeBot);

            var host = CreateHostBuilder().Build();
            var logger = Log.Logger.ForContext(typeof(calledudeBot));

            var services = host.Services;
            await host.Services
                .GetRequiredService<DatabaseContext>()
                .Database.MigrateAsync();

            logger.Information("Migrations applied");

            var commandContainer = services.GetRequiredService<CommandContainer>();

            logger.Information($"Done. Loaded {commandContainer.Commands.Count} commands.");

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services
                        .AddTwitchConfigs()
                        .AddBots()
                        .AddCommands()
                        .AddServices()
                        .AddDatabase()
                        .AddLazyResolution()
                        .AddHostedService(x => x.GetRequiredService<IMessageBot<DiscordMessage>>())
                        .AddHostedService(x => x.GetRequiredService<IMessageBot<IrcMessage>>())
                        .AddHostedService(x => x.GetRequiredService<ITwitchUser>())
                        .AddHostedService(x => x.GetRequiredService<SteamBot>());
                })
                .ConfigureLogging((_, logging) =>
                {
                    logging.ClearProviders();
                    var logger = new LoggerConfiguration()
                        .WriteTo.Console(LogEventLevel.Verbose, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .CreateLogger();
                    logging.AddSerilog(logger);
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
        }
    }
}
