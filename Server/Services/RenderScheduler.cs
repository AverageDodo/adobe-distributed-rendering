using DistributedRendering.AME.Server.Hubs;
using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Interfaces;

using Microsoft.AspNetCore.SignalR;

namespace DistributedRendering.AME.Server.Services;

internal sealed class RenderScheduler(
	ILogger<RenderScheduler> logger,
	IConfigurationService configurationService,
	IRenderQueueService renderQueueService,
	IHubContext<RenderHub, IRenderClient> renderHub
) : BackgroundService
{
	private RenderSchedulerSettings Settings => configurationService.RenderSchedulerSettings;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				IRenderFragment? data = await renderQueueService.DequeueFragmentAsync(stoppingToken);

				if (data is null)
				{
					await Task.Delay(Settings.LoopTimeout, stoppingToken);

					continue;
				}

				string? renderNodeConnectionId = RenderHub.GetRenderNode(data);

				if (string.IsNullOrWhiteSpace(renderNodeConnectionId))
				{
					renderQueueService.QueueFragment(data);

					logger.LogTrace("Failed to acquire a connection ID for a render node.");
					await Task.Delay(Settings.LoopTimeout, stoppingToken);

					continue;
				}

				var wasRenderStarted = false;

				try
				{
					wasRenderStarted = await renderHub.Clients.Client(renderNodeConnectionId)
						.StartRenderJob(data);
				}
				catch (Exception e)
				{
					logger.LogError(
						e,
						"Exception caught while attempting to send render fragment '{Guid}' to client.",
						data.Guid
					);
				}

				if (!wasRenderStarted)
				{
					logger.LogWarning(
						"Sent render job '{Guid}' to the render client, but response was negative.",
						data.Guid
					);

					renderQueueService.QueueFragment(data);
				}
				else
				{
					logger.LogInformation(
						"Render fragment '{Guid}' was assigned to client '{Id}'.",
						data.Guid,
						renderNodeConnectionId
					);
				}
			}
		}
		catch (OperationCanceledException)
		{
			logger.LogInformation("Background service cancellation token invoked. Service loop exiting...");
		}
	}
}