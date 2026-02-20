using System.Collections.Concurrent;

using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;
using DistributedRendering.AME.Shared.Util;

using Microsoft.AspNetCore.SignalR;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DistributedRendering.AME.Server.Hubs;

// https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-10.0
public sealed class RenderHub(
	ILogger<RenderHub> logger,
	IDatabaseService databaseService,
	IRenderQueueService queueService
) : Hub<IRenderClient>, IRenderHub
{
	private static readonly ConcurrentDictionary<string, ClientStatus> ConnectedRenderNodes = [];
	private static readonly ConcurrentDictionary<string, IRenderFragment> AssignedFragments = [];

	public Task OnClientStatusUpdate(ClientStatus updatedStatus)
	{
		string connectionId = this.Context.ConnectionId;

		logger.LogInformation(
			"Client with id '{ConnectionId}' updated its status to '{Status}'",
			connectionId,
			updatedStatus
		);

		if (!string.IsNullOrWhiteSpace(connectionId))
			ConnectedRenderNodes.AddOrUpdate(
				connectionId,
				updatedStatus,
				(_, _) => updatedStatus
			);

		return Task.CompletedTask;
	}

	public async Task ReceiveRenderReport(string reportJson)
	{
		RenderReport? report = null;
		try
		{
			report = JsonSerializer.Deserialize<RenderReport>(reportJson, JsonConfig.SerializerOptions);
			logger.LogDebug("Render report received: \n{Report}", reportJson);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Failed to deserialize instance of render report. JSON string was:\n{Json}", reportJson);
		}

		if (report is null)
		{
			logger.LogWarning("Could not parse render report JSON, assuming failure. Fragment will be re-queued.");

			if (!AssignedFragments.TryRemove(this.Context.ConnectionId, out IRenderFragment? fragment))
				logger.LogCritical(
					"Render report could not be parsed and no fragment could be found for the nodes connectionID. Fragment might have been lost and will block the request from ever completing."
				);
			else
				queueService.QueueFragment(fragment);

			return;
		}

		AssignedFragments.TryRemove(this.Context.ConnectionId, out _);
		bool wasReportInserted = await databaseService.InsertRenderReportAsync(report);

		if (!wasReportInserted)
			logger.LogWarning(
				"Could not insert render report into database. Report was: \n{Report}",
				JsonSerializer.Serialize(report, JsonConfig.SerializerOptions)
			);

		if (report.Status.HasFlag(RenderStatus.Failed))
		{
			logger.LogWarning(
				"Received render report that indicates render failure and will be re-queued. Fragment with GUID '{Guid}' included error message '{Msg}'.",
				report.Fragment.Guid,
				report.ErrorMessage
			);

			queueService.QueueFragment(report.Fragment);

			return;
		}

		// var timestamps = new RenderReportTimestamps(report);
		//
		// var sb = new StringBuilder();
		// sb.AppendLine($"Render fragment '{report.Fragment.Guid}' completed by node '{this.Context.ConnectionId}'.");
		// sb.AppendLine($"Loading time was '{timestamps.LoadingTime:g}'");
		// sb.AppendLine($"Rendering time was '{timestamps.RenderingTime:g}'");
		// sb.AppendLine($"Encoding time was '{timestamps.EncodingTime:g}'");
		// sb.AppendLine($"Time-to-Export was '{timestamps.TimeToExport:g}'");
		//
		// logger.LogInformation(sb.ToString());

		if (!wasReportInserted)
			logger.LogWarning("Failed to insert render report for fragment '{Guid}'.", report.Fragment.Guid);
	}

	internal static string? GetRenderNode(IRenderFragment fragment)
	{
		if (ConnectedRenderNodes.IsEmpty) return null;

		(string? connectionId, ClientStatus clientStatus) = ConnectedRenderNodes
			.FirstOrDefault(node => node.Value == ClientStatus.Ready);

		if (string.IsNullOrWhiteSpace(connectionId))
			return null;

		bool wasNodeStatusUpdated = ConnectedRenderNodes.TryUpdate(connectionId, ClientStatus.Busy, clientStatus);
		bool wasFragmentAssigned = AssignedFragments.TryAdd(connectionId, fragment);

		if (!wasNodeStatusUpdated || !wasFragmentAssigned)
		{
			ConnectedRenderNodes.TryUpdate(connectionId, ClientStatus.Ready, clientStatus);
			AssignedFragments.TryRemove(connectionId, out _);

			return null;
		}

		return connectionId;
	}

	public override async Task OnConnectedAsync()
	{
		logger.LogInformation(
			"New client with id '{Id}' connected.",
			this.Context.ConnectionId
		);

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		string connectionId = this.Context.ConnectionId;
		if (exception is null)
			logger.LogInformation("Client with id '{Id}' disconnected.", connectionId);
		else
			logger.LogError(exception, "Connection with client lost due to an unexpected error.");

		if (!string.IsNullOrWhiteSpace(connectionId))
		{
			ConnectedRenderNodes.TryRemove(connectionId, out _);

			if (AssignedFragments.TryRemove(connectionId, out IRenderFragment? fragment))
				queueService.QueueFragment(fragment);
		}

		await base.OnDisconnectedAsync(exception);
	}
}