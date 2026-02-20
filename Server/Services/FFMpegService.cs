using System.Security.Cryptography;

using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Server.Services.Interfaces;

using FFMpegCore;
using FFMpegCore.Exceptions;

namespace DistributedRendering.AME.Server.Services;

// ReSharper disable once InconsistentNaming
internal sealed class FFMpegService(
	ILogger<FFMpegService> logger,
	IConfigurationService configurationService,
	IRenderQueueService queueService,
	IDatabaseService databaseService
) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		string? binFolder = configurationService.FFMpegSettings.BinariesFolder;
		if (!string.IsNullOrWhiteSpace(binFolder))
			GlobalFFOptions.Configure(
				new FFOptions { BinaryFolder = binFolder }
			);

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				Guid guid = await queueService.DequeueReassembly(stoppingToken);

				if (guid == Guid.Empty) continue;

				logger.LogInformation("Starting reassembly for request '{Guid}'.", guid);
				FileInfo? finalFile = await AssembleFinalFileAsync(guid, stoppingToken);

				if (finalFile is not { Exists: true })
				{
					logger.LogError("The fragments for request '{Guid}' could not be joined.", guid);

					continue;
				}

				logger.LogInformation(
					"Successfully joined all fragments for request '{Guid}'. Output path is '{Path}'.",
					guid,
					finalFile.FullName
				);
			}
		}
		catch (OperationCanceledException)
		{
			logger.LogInformation("Background service cancellation token invoked. Service loop exiting...");
		}
	}

	private async Task<FileInfo?> AssembleFinalFileAsync(Guid requestGuid, CancellationToken token)
	{
		FileInfo outputFile = (await databaseService.GetRenderRequestAsync(requestGuid, token))?.OutputPath!;
		ICollection<DbFragment> fragments = await databaseService.GetFragmentsForRequestAsync(requestGuid, token);

		if (fragments.Count == 0)
		{
			logger.LogWarning(
				"The database service returned no valid fragment files for request '{Guid}'.",
				requestGuid
			);

			return null;
		}

		string[] paths = [.. fragments.Select(fragment => fragment.OutputPath.FullName)];

		if (!outputFile.Exists)
		{
			await using FileStream fs = outputFile.Create();

			fs.Close();
		}

		outputFile.Refresh();
		var wasJoined = false;
		List<(string tempFileName, string[] batch)> batches = paths
			.Chunk(5)
			.Select(batch => ($"{RandomNumberGenerator.GetHexString(3)}.mp4", batch))
			.ToList();

		try
		{
			List<Task> joinTasks = [];
			joinTasks.AddRange(
				batches.Select(pair => Task.Run(() => { FFMpeg.Join(pair.tempFileName, pair.batch); }, token))
			);

			await Task.WhenAll(joinTasks);

			wasJoined = FFMpeg.Join(
				outputFile.FullName,
				batches
					.Select(pair => pair.tempFileName)
					.ToArray()
			);
		}
		catch (FFMpegException e)
		{
			logger.LogError(
				e,
				"FFMpeg threw an exception while attempting to join the fragments for request '{Guid}'.",
				requestGuid
			);
		}

		if (!wasJoined)
		{
			logger.LogWarning("Failed to join fragments for request '{Guid}'.", requestGuid);

			return null;
		}

		await databaseService.MarkRequestCompleteAsync(requestGuid, DateTime.UtcNow, token);
		logger.LogInformation("Successfully joined fragments for request '{Guid}'.", requestGuid);

		if (!configurationService.FileSettings.DeleteFragmentsAfterReassembly)
			return outputFile;

		foreach (string path in paths)
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Exception caught while attempting to delete fragment file after final reassembly.");
			}

		return outputFile;
	}
}