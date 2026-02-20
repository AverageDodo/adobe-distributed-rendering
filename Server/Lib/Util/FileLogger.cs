using System.Collections.Concurrent;
using System.Text;

using DistributedRendering.AME.Server.Services.Interfaces;

using Microsoft.Extensions.Logging.Abstractions;

namespace DistributedRendering.AME.Server.Lib.Util;

public sealed class FileLogger(
	string categoryName,
	FileLoggerSettings settings
) : ILogger
{
	internal static readonly ConcurrentQueue<string> LogMessageQueue = [];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return null;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel >= settings.MinimumLogLevel && logLevel != LogLevel.None;
	}

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter
	)
	{
		if (!IsEnabled(logLevel))
			return;

		ArgumentNullException.ThrowIfNull(formatter);

		string message = formatter(state, exception);

		if (string.IsNullOrWhiteSpace(message) && exception is null)
			return;

		LogMessageQueue.Enqueue(BuildLogLine(logLevel, message, exception));
	}

	private string BuildLogLine(
		LogLevel logLevel,
		string message,
		Exception? exception
	)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"[{DateTime.UtcNow:s}] [{logLevel}] - [{categoryName}]");
		sb.AppendLine(message);

		if (exception is not null)
		{
			sb.AppendLine(exception.Message);
			sb.AppendLine(exception.StackTrace);
		}

		sb.AppendLine();

		return sb.ToString();
	}
}

public sealed class FileLoggerProvider(
	FileLoggerSettings configuration
) : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName)
	{
		return configuration.Enabled ? new FileLogger(categoryName, configuration) : NullLogger.Instance;
	}

	public void Dispose() { }
}