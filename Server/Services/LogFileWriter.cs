using System.Text;

using DistributedRendering.AME.Server.Lib.Util;
using DistributedRendering.AME.Server.Services.Interfaces;

namespace DistributedRendering.AME.Server.Services;

public sealed class FileLogWriter(
	FileLoggerSettings configuration
) : BackgroundService
{
	private const string c_LogFileSeparator = "==================================================\n";
	private readonly DirectoryInfo logFileDirectory = new(configuration.LogFileDirectory);

	private FileInfo logFile =
		new(Path.Combine(configuration.LogFileDirectory, $"{DateTime.UtcNow:yyyy-MM-dd_hh-mm-ss}.log"));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			if (!logFileDirectory.Exists)
				logFileDirectory.Create();

			await File.AppendAllTextAsync(
				GetLogFile().FullName,
				c_LogFileSeparator,
				Encoding.UTF8,
				CancellationToken.None
			);

			while (!stoppingToken.IsCancellationRequested)
			{
				if (!FileLogger.LogMessageQueue.TryDequeue(out string? logMessage))
				{
					await Task.Delay(configuration.LoopTimeout, stoppingToken);

					continue;
				}

				FileInfo currentLogFile = GetLogFile();
				await File.AppendAllTextAsync(
					currentLogFile.FullName,
					logMessage,
					Encoding.UTF8,
					CancellationToken.None
				);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	private FileInfo GetLogFile()
	{
		if (!logFile.Exists || logFile.Length < configuration.MaxFileSizeInBytes)
			return logFile;

		FileInfo[] logFiles = logFileDirectory.GetFiles("*.log");
		try
		{
			if (logFiles.Length > 10)
				logFiles.OrderBy(file => file.CreationTimeUtc).FirstOrDefault()?.Delete();
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to delete old log file.\n\n{e}");
		}

		logFile = new FileInfo(Path.Combine(logFileDirectory.FullName, $"{DateTime.UtcNow:yyyy-MM-dd_hh_mm_ss}.log"));

		return logFile;
	}
}