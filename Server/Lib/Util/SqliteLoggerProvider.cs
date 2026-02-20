using DistributedRendering.AME.Server.Services.Interfaces;

namespace DistributedRendering.AME.Server.Lib.Util;

public class SqliteLoggerProvider(DatabaseLoggerSettings settings) : ILoggerProvider
{
	private readonly SqliteLogDatabaseProvider databaseProvider = new(settings);

	public ILogger CreateLogger(string categoryName)
	{
		return new SqliteLogger(categoryName, settings.MinimumLogLevel, databaseProvider);
	}

	public void Dispose()
	{
		databaseProvider.DisposeAsync().AsTask().Wait();
	}
}