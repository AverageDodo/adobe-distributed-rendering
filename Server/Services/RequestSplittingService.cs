using DistributedRendering.AME.Server.Lib.Interfaces;
using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;

namespace DistributedRendering.AME.Server.Services;

internal sealed class RequestSplittingService(
	ILogger<RequestSplittingService> logger,
	IDatabaseService databaseService,
	IRenderQueueService queueService,
	IFileManagementService fileManagementService
) : IRequestSplittingService
{
	public async Task SplitRequestAsync(
		IRenderRequest request,
		TimeSpan jobLength,
		CancellationToken token = default
	)
	{
		List<RenderFragment> fragments = [];
		var jobLengthInMilliseconds = (long)jobLength.TotalMilliseconds;
		var totalLength = (long)request.TimelineDuration.TotalMilliseconds;
		long remainingTime = totalLength;
		ushort index = 0;

		while (remainingTime > 0 || index == ushort.MaxValue)
			try
			{
				var fragmentGuid = Guid.NewGuid();
				string fragmentOutputPath = Path.Combine(
					request.RequestDirectory.FullName,
					$"{fragmentGuid}.mp4"
				);

				long fragmentLength = remainingTime < jobLengthInMilliseconds ? remainingTime : jobLengthInMilliseconds;
				var job = new RenderFragment
				{
					Guid = fragmentGuid,
					RequestGuid = request.Guid,
					ProjectPath = request.ProjectPath,
					PresetPath = request.EncoderPresetPath,
					DestinationPath = fragmentOutputPath,
					Index = index,
					StartTimeInMilliseconds = totalLength - remainingTime,
					DurationInMilliseconds = (int)fragmentLength
				};

				if (!await databaseService.InsertFragmentAsync(job, token))
					logger.LogWarning("Failed to insert render fragment '{Guid}' into the database.", job.Guid);

				queueService.QueueFragment(job);
				fragments.Add(job);

				remainingTime -= fragmentLength;
				index++;
			}
			catch (Exception e)
			{
				logger.LogError(e, "Exception caught while attempting to generate JobData instance.");

				break;
			}

		logger.LogInformation(
			"Created '{Amount}' fragment(s) for request '{Guid}'.",
			index,
			request.Guid
		);

		List<FileInfo> requiredFiles = fragments
			.Select(fragment => new FileInfo(fragment.DestinationPath))
			.ToList();

		if (!fileManagementService.CreateWatcherForRequest(request, requiredFiles))
			logger.LogWarning(
				"Failed to create a FileSystemWatcher for request '{Guid}' at '{Path}'.",
				request.Guid,
				request.RequestDirectory
			);
	}
}