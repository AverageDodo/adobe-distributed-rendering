using System.Text.Json;
using System.Text.Json.Serialization;

using DistributedRendering.AME.Server.Hubs;
using DistributedRendering.AME.Server.Lib.Util;
using DistributedRendering.AME.Server.Services;
using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.Util;

using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi;

namespace DistributedRendering.AME.Server;

public static class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		CheckPrerequisites(builder.Configuration);

		builder.Logging.ClearProviders();
		builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>(options =>
			{
				options.UseUtcTimestamp = true;
			}
		);

		var fileLoggerSettings = builder.Configuration.GetSection("Logging:File").Get<FileLoggerSettings>()!;
		var databaseLoggerSettings =
			builder.Configuration.GetSection("Logging:Database").Get<DatabaseLoggerSettings>()!;

		builder.Logging.AddDebug();
		builder.Logging.AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));
		builder.Logging.AddProvider(new FileLoggerProvider(fileLoggerSettings));
		builder.Logging.AddProvider(new SqliteLoggerProvider(databaseLoggerSettings));

		builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<IFileManagementService, FileManagementService>();
		builder.Services.AddSingleton<IRenderQueueService, RenderQueueService>();

		builder.Services.AddHostedService<DiscoveryService>();
		builder.Services.AddHostedService<RenderScheduler>();
		builder.Services.AddHostedService<FFMpegService>();
		builder.Services.AddHostedService<FileLogWriter>(_ => new FileLogWriter(fileLoggerSettings));

		builder.Services.AddTransient<IRequestSplittingService, RequestSplittingService>();

		builder.Services
			.AddSignalR(signalROptions =>
				{
					signalROptions.EnableDetailedErrors = true;
					signalROptions.ClientTimeoutInterval = TimeSpan.FromSeconds(1000);
				}
			)
			.AddJsonProtocol(jsonProtocolOptions =>
				{
					jsonProtocolOptions.PayloadSerializerOptions = JsonConfig.SerializerOptions;
				}
			);

		builder.Services
			.AddControllers()
			.AddJsonOptions(jsonOptions =>
				{
					jsonOptions.JsonSerializerOptions.AllowTrailingCommas = true;
					jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
					jsonOptions.JsonSerializerOptions.WriteIndented = true;
					jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
					jsonOptions.JsonSerializerOptions.Converters.Add(new DirectoryInfoJsonConverter());
					jsonOptions.JsonSerializerOptions.Converters.Add(new FileInfoJsonConverter());
					jsonOptions.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
				}
			);


		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(swaggerGenOptions =>
			{
				// Custom filter to remove reflection-based objects from response views.
				// Makes the swagger UI orders of magnitude faster.
				swaggerGenOptions.SchemaGeneratorOptions.SchemaFilters.Add(new SchemasFilter());

				swaggerGenOptions.SwaggerDoc(
					"v1",
					new OpenApiInfo { Title = "AME Distributed Rendering: Server API" }
				);
			}
		);


		WebApplication app = builder.Build();

		app.UseSwagger();
		app.UseSwaggerUI(config =>
			{
				config.SwaggerEndpoint("/swagger/v1/swagger.json", "AME Render Server");
				config.EnableTryItOutByDefault();
			}
		);

		app.UseDeveloperExceptionPage();

		app.MapControllers();
		app.MapHub<RenderHub>("/render");

		app.Run();
	}

	private static void CheckPrerequisites(IConfiguration configuration)
	{
		var configManager = new ConfigurationService(configuration);

		try
		{
			DirectoryInfo rootDir = configManager.FileSettings.RootDataDirectory;

			if (!rootDir.Exists)
				throw new DirectoryNotFoundException(
					$"The configured root data directory does not exist at path '{rootDir.FullName}'."
				);

			DirectoryInfo projectsDir = configManager.FileSettings.ProjectsDirectory;

			if (!projectsDir.Exists) projectsDir.Create();
			projectsDir.Refresh();

			if (!projectsDir.Exists)
				throw new DirectoryNotFoundException(
					$"The required projects directory does not exist at path '{projectsDir.FullName}' and could not be created."
				);

			DirectoryInfo requestsDir = configManager.FileSettings.RequestsDirectory;

			if (!requestsDir.Exists) requestsDir.Create();
			requestsDir.Refresh();

			if (!requestsDir.Exists)
				throw new DirectoryNotFoundException(
					$"The required requests directory does not exist at path '{requestsDir.FullName}' and could not be created."
				);
		}
		catch (Exception e)
		{
			Console.WriteLine("Exception occured during prerequisite checks. Press any key to terminate...");
			Console.WriteLine(e);
			Console.ReadKey();

			throw;
		}
	}
}