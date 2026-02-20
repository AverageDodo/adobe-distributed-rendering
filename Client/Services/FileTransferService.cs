using System.Collections.Concurrent;

namespace DistributedRendering.AME.Client.Services;

internal sealed class FileTransferService(
	ILogger<FileTransferService> logger
) : BackgroundService
{
	private static readonly ConcurrentQueue<(FileInfo, FileInfo, string)> FileTransferQueue = [];

	public static void QueueFileTransfer(FileInfo sourceFile, FileInfo destinationFile, string newFileName)
	{
		if (!sourceFile.Exists || destinationFile.Exists || string.IsNullOrWhiteSpace(newFileName))
			throw new ArgumentException(
				"The source file must exist. The destination file must NOT exist. The new file name cannot be null or empty."
			);

		FileTransferQueue.Enqueue((sourceFile, destinationFile, newFileName));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if (!FileTransferQueue.TryDequeue(out (FileInfo source, FileInfo destination, string newName) result))
				{
					await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

					continue;
				}

				_ = Task.Run(
					async () =>
					{
						try
						{
							logger.LogDebug(
								"Initiating file transfer. \nSource file is '{Source}'. \nDestination file is '{Dest}'.\nFinal rename is '{Rename}'.",
								result.source.FullName,
								result.destination.FullName,
								result.newName
							);

							// Move the file from local storage to NAS with an unknown file name
							File.Move(result.source.FullName, result.destination.FullName);

							while (!result.destination.Exists)
							{
								await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
								result.destination.Refresh();
							}

							// Rename to correct file name to trigger FileSystemWatcher of Render Hub
							var finalOutputFile = new FileInfo(
								Path.Combine(
									result.destination.DirectoryName!,
									result.newName
								)
							);

							File.Move(
								result.destination.FullName,
								finalOutputFile.Name
							);

							while (!finalOutputFile.Exists)
							{
								await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
								finalOutputFile.Refresh();
							}

							logger.LogInformation(
								"Completed fragment file transferred successfully to '{Path}'.",
								finalOutputFile.FullName
							);
						}
						catch (Exception e)
						{
							logger.LogError(e, "Exception caught while attempting to transfer file.");
						}
					},
					stoppingToken
				);
			}
		}
		catch (OperationCanceledException) { }
	}
}