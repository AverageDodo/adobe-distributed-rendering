using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace DistributedRendering.AME.Server.Lib.Util;

public sealed class CustomConsoleFormatter() : ConsoleFormatter(nameof(CustomConsoleFormatter))
{
	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter
	)
	{
		LogLevel logLevel = logEntry.LogLevel;
		string category = logEntry.Category;
		string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

		string colorCode = logLevel switch
		{
			LogLevel.Trace => AnsiColors.c_Gray,
			LogLevel.Debug => AnsiColors.c_Cyan,
			LogLevel.Information => AnsiColors.c_Green,
			LogLevel.Warning => AnsiColors.c_Yellow,
			LogLevel.Error => AnsiColors.c_Red,
			LogLevel.Critical => AnsiColors.c_Magenta,
			_ => AnsiColors.c_White
		};

		var logMessage = $"\n[{DateTime.UtcNow:s}] [{logLevel}] {category}: \n  {message}";
		textWriter.WriteLine(
			$"{colorCode}{logMessage}{AnsiColors.c_Reset}"
		);

		if (logEntry.Exception != null)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			var exceptionMessage = $"\n{logEntry.Exception}\n";
			textWriter.WriteLine("{0}{1}{2}", AnsiColors.c_Red, exceptionMessage, AnsiColors.c_Reset);
		}
	}

	private readonly record struct AnsiColors
	{
		internal const string c_Reset = "\e[0m";
		internal const string c_White = "\e[37m";
		internal const string c_Gray = "\e[90m";
		internal const string c_Cyan = "\e[36m";
		internal const string c_Green = "\e[32m";
		internal const string c_Yellow = "\e[33m";
		internal const string c_Red = "\e[31m";
		internal const string c_Magenta = "\e[35m";
	}
}