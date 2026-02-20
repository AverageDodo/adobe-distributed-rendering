using System.Text.Json;

using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;
using DistributedRendering.AME.Shared.Util;

using Microsoft.AspNetCore.SignalR.Client;

namespace DistributedRendering.AME.Client.Services;

internal sealed class SignalRClient : IRenderClient, IAsyncDisposable
{
	private readonly IConnectionManagerService connectionManagerService;
	private readonly ILogger<SignalRClient> logger;
	private readonly IServiceProvider serviceProvider;
	private readonly IStateService stateService;

	private HubConnection? renderHubConnection;

	public SignalRClient(
		ILogger<SignalRClient> logger,
		IStateService stateService,
		IConnectionManagerService connectionManagerService,
		IServiceProvider serviceProvider
	)
	{
		this.logger = logger;
		this.stateService = stateService;
		this.serviceProvider = serviceProvider;
		this.connectionManagerService = connectionManagerService;

		this.stateService.StatusChangedAsync += async (_, status) => await SendStatusUpdateToHub(status);

		_ = Task.Run(async () => await StartConnectionAsync());
	}

	private bool IsConnected => renderHubConnection is { State: HubConnectionState.Connected };

	private async Task StartConnectionAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		logger.LogInformation("Attempting to create a connection to the hub...");
		try
		{
			while (renderHubConnection is null)
				renderHubConnection = await connectionManagerService.CreateHubConnectionAsync(cancellationToken);

			await renderHubConnection.StartAsync(cancellationToken);

			// Not the best way to do this but this prevents a race condition that can cause
			// the client status to become locked as 'Busy' on the server-side.
			await Task.Delay(500, CancellationToken.None);
			stateService.SetStatus(ClientStatus.Busy, out _);

			await Task.Delay(500, CancellationToken.None);
			stateService.SetStatus(ClientStatus.Ready, out _);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception occured during hub connection start.");

			if (renderHubConnection is null)
				return;
		}

		renderHubConnection.Closed += OnClosed;
		renderHubConnection.Reconnecting += OnReconnecting;
		renderHubConnection.Reconnected += OnReconnect;

		renderHubConnection.On(nameof(IRenderClient.GetClientStatus), GetClientStatus);
		renderHubConnection.On<RenderFragment, bool>(nameof(StartRenderJob), StartRenderJob);

		logger.LogInformation("Hub connection started.");
	}

	public async Task DisposeHubConnectionAsync(CancellationToken cancellationToken = default)
	{
		if (renderHubConnection is null)
			return;

		try
		{
			if (renderHubConnection is not { State: HubConnectionState.Disconnected })
				await renderHubConnection.StopAsync(cancellationToken);

			renderHubConnection.Remove(nameof(GetClientStatus));
			renderHubConnection.Remove(nameof(StartRenderJob));

			renderHubConnection.Reconnecting -= OnReconnecting;
			renderHubConnection.Reconnected -= OnReconnect;
			renderHubConnection.Closed -= OnClosed;

			await renderHubConnection.DisposeAsync();
		}
		catch (ObjectDisposedException) { }
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to dispose of HubConnection instance.");
		}
		finally
		{
			renderHubConnection = null;
		}
	}

	#region SignalR Client Methods

	public Task<ClientStatus> GetClientStatus()
	{
		return Task.FromResult(stateService.GetStatus(out ClientStatus status) ? status : ClientStatus.Unavailable);
	}

	public async Task<bool> StartRenderJob(IRenderFragment data)
	{
		stateService.SetStatus(ClientStatus.Busy, out _);
		logger.LogDebug("Render fragment: \n{Fragment}", JsonSerializer.Serialize(data, JsonConfig.SerializerOptions));

		var outputDir = new DirectoryInfo(new FileInfo(data.DestinationPath).DirectoryName!);
		if (!outputDir.Exists)
		{
			logger.LogDebug("Creating output directory for render fragment at '{Path}'.", outputDir.FullName);
			Directory.CreateDirectory(outputDir.FullName);
		}

		var encoderService = serviceProvider.GetService<IMediaEncoderService>()!;
		encoderService.RenderCompleteEvent += OnRenderComplete;
		bool wasRenderStarted = await encoderService.StartRenderProcessAsync(data);

		if (!wasRenderStarted)
		{
			await encoderService.DisposeAsync();
			stateService.SetStatus(ClientStatus.Ready, out _);
		}

		return wasRenderStarted;
	}

	#endregion

	#region SignalR Hub Methods

	private async Task SendStatusUpdateToHub(ClientStatus updatedStatus)
	{
		if (!IsConnected)
			await StartConnectionAsync();

		try
		{
			if (renderHubConnection!.State != HubConnectionState.Connected)
				return;

			logger.LogTrace("Sending status update to hub...");
			await renderHubConnection.InvokeAsync(nameof(IRenderHub.OnClientStatusUpdate), updatedStatus);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to send status update to the server.");
		}
	}

	private async void OnRenderComplete(
		object? sender,
		IRenderReport report
	)
	{
		try
		{
			if (!IsConnected)
				await StartConnectionAsync();

			// SignalR requires a serializable instance type. Supplying an interface type is not allowed.
			if (report is not RenderReport renderReport)
				throw new InvalidCastException(
					$"Failed to cast instance of '{nameof(IRenderReport)}' to required type '{typeof(RenderReport).FullName}'."
				);

			string reportJson = JsonSerializer.Serialize(renderReport, JsonConfig.SerializerOptions);
			await renderHubConnection!.InvokeAsync(nameof(IRenderHub.ReceiveRenderReport), reportJson);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to notify hub of completed render.");
		}
		finally
		{
			stateService.SetStatus(ClientStatus.Ready, out _);

			if (sender is IMediaEncoderService encoderService)
			{
				encoderService.RenderCompleteEvent -= OnRenderComplete;
				await encoderService.DisposeAsync();
			}
		}
	}

	#endregion

	#region SignalR Callbacks

	private Task OnReconnecting(Exception? e)
	{
		if (e is not null)
			logger.LogError(e, "Exception occured while attempting to reconnect to the hub.");
		else
			logger.LogDebug("Attempting to reconnect to the hub...");

		return Task.CompletedTask;
	}

	private async Task OnReconnect(string? connectionId)
	{
		logger.LogInformation("Successfully reconnected to the hub.");

		stateService.GetStatus(out ClientStatus status);
		await SendStatusUpdateToHub(status);
	}

	private async Task OnClosed(Exception? exception)
	{
		if (exception is not null)
			logger.LogError(exception, "The SignalR connection with the hub was closed unexpectedly.");
		else
			logger.LogInformation("SignalR hub connection closed.");

		await DisposeHubConnectionAsync();

		_ = Task.Run(async () => await StartConnectionAsync());
	}

	#endregion

	public async ValueTask DisposeAsync()
	{
		await DisposeHubConnectionAsync();
	}
}