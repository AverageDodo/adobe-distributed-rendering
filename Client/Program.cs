using DistributedRendering.AME.Client.Lib.Util;
using DistributedRendering.AME.Client.Services;
using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.Interfaces;

using Microsoft.Extensions.Logging.Console;

namespace DistributedRendering.AME.Client;

public static class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.Logging.ClearProviders();
		builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>(options =>
			{
				options.UseUtcTimestamp = true;
			}
		);

		builder.Logging.AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));

		CheckPrerequisites(builder.Configuration);

		builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
		builder.Services.AddSingleton<IStateService, StateService>();
		builder.Services.AddSingleton<IRenderClient, SignalRClient>();
		builder.Services.AddSingleton<IConnectionManagerService, ConnectionManagerService>();

		builder.Services.AddHostedService<FileTransferService>();

		builder.Services.AddTransient<IMediaEncoderService, MediaEncoderService>();
		builder.Services.AddTransient<IScriptGenerationService, ScriptGenerationService>();

		WebApplication app = builder.Build();

		// Resolve SignalRClient to ensure it is instantiated
		_ = app.Services.GetRequiredService<IRenderClient>();

		app.Run();
	}

	private static void CheckPrerequisites(IConfiguration configuration)
	{
		var cfg = new ConfigurationService(configuration);

		if (!cfg.ProgramDataDirectory.Exists)
			cfg.ProgramDataDirectory.Create();

		if (!cfg.FileSettings.AppdataDirectory.Exists)
			cfg.FileSettings.AppdataDirectory.Create();
	}
}