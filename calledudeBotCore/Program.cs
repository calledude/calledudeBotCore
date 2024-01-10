using calledudeBot.Bots;
using calledudeBot.Chat;
using calledudeBot.Chat.Commands;
using calledudeBot.Config;
using calledudeBot.Database;
using calledudeBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot;

public static class Program
{
	public static async Task Main()
	{
		Console.Title = nameof(calledudeBot);

		var host = CreateHostBuilder().Build();

		var services = host.Services;

		var loggerFactory = services.GetRequiredService<ILoggerFactory>();
		var logger = loggerFactory.CreateLogger(typeof(Program));

		var database = services
			.GetRequiredService<DatabaseContext>().Database;

		var pendingMigrations = await database.GetPendingMigrationsAsync();

		if (pendingMigrations.Any())
		{
			await database.MigrateAsync();
			logger.LogInformation("{migrationCount} Migration(s) applied", pendingMigrations.Count());
		}

		var commandContainer = services.GetRequiredService<ICommandContainer>();
		logger.LogInformation("Done. Loaded {numberOfCommands} commands.", commandContainer.Commands.Count);

		await host.RunAsync();
	}

	private static IHostBuilder CreateHostBuilder()
		=> Host.CreateDefaultBuilder()
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
					.AddHostedService(x => x.GetRequiredService<ITwitchBot>())
					.AddHostedService(x => x.GetRequiredService<ITwitchUser>())
					.AddHostedService(x => x.GetRequiredService<IOsuBot>())
					.AddHostedService(x => x.GetRequiredService<ISteamBot>())
					.AddHostedService(x => x.GetRequiredService<IWorkItemQueueService>());
			})
			.ConfigureLogging((_, logging) =>
			{
				logging.ClearProviders();
				var logger = new LoggerConfiguration()
					.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
					.MinimumLevel.Is(LogEventLevel.Verbose)
					.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
					.MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
					.Enrich.FromLogContext()
					.CreateLogger();
				logging.AddSerilog(logger);
			});
}