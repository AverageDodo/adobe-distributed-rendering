namespace DistributedRendering.AME.Server.Lib.Util;

public class SqliteLogger(string categoryName, LogLevel minimumLogLevel, SqliteLogDatabaseProvider dbProvider) : ILogger
{
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return null;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel >= minimumLogLevel;
	}

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter
	)
	{
		string message = formatter(state, exception);
		string? exceptionString = exception is null
			? null
			: $"Message: \n{exception.Message}\n Stack Trace:\n{exception.StackTrace}";

		dbProvider.QueueLogEntry(
			new DbLogEntry(
				DateTime.UtcNow.ToString("O"),
				(uint)logLevel,
				categoryName,
				message,
				exceptionString
			)
		);
	}
}