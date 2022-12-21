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
using System.Threading.Tasks;

namespace calledudeBot;

public static class Program
{
	public static async Task Main()
	{
		Console.Title = nameof(calledudeBot);

		var host = CreateHostBuilder().Build();

		var services = host.Services;
		await services
			.GetRequiredService<DatabaseContext>()
			.Database.MigrateAsync();

		Log.Logger.Information("Migrations applied");

		var commandContainer = services.GetRequiredService<ICommandContainer>();
		Log.Logger.Information($"Done. Loaded {commandContainer.Commands.Count} commands.");

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
					.AddHostedService(x => x.GetRequiredService<ISteamBot>());
			})
			.ConfigureLogging((_, logging) =>
			{
				logging.ClearProviders();
				var logger = new LoggerConfiguration()
					.WriteTo.Console(LogEventLevel.Verbose, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
					.MinimumLevel.Verbose()
					.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
					.MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
					.Enrich.FromLogContext()
					.CreateLogger();
				logging.AddSerilog(logger);
				logging.SetMinimumLevel(LogLevel.Debug);
			});
}
