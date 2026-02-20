using System.Net;
using System.Net.Sockets;
using System.Text;

using DistributedRendering.AME.Client.Services.Interfaces;

using Microsoft.AspNetCore.SignalR.Client;

namespace DistributedRendering.AME.Client.Services;

internal sealed class ConnectionManagerService(
	ILogger<ConnectionManagerService> logger,
	IConfigurationService configurationService
) : IConnectionManagerService
{
	public async Task<HubConnection?> CreateHubConnectionAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return null;

		Uri? hubUri = await GetHubUriAsync(cancellationToken);
		if (hubUri is not { IsAbsoluteUri: true })
		{
			logger.LogWarning("Failed to obtain a valid URI for the render hub.");

			return null;
		}

		HubConnection renderHubConnection = new HubConnectionBuilder()
			.WithKeepAliveInterval(
				Timeout.InfiniteTimeSpan
			)
			.WithAutomaticReconnect()
			.WithUrl(hubUri.AbsoluteUri)
			.Build();

		return renderHubConnection;
	}

	private async Task<Uri?> GetHubUriAsync(CancellationToken cancellationToken = default)
	{
		IPEndPoint? endpoint = await DiscoverServerAsync(cancellationToken);
		while (endpoint is null)
		{
			logger.LogWarning("Failed to acquire server url from UDP discovery. Retrying...");
			endpoint = await DiscoverServerAsync(cancellationToken);
		}

		logger.LogInformation(
			"Received UDP response from server at '{Address}'.",
			endpoint.Address.ToString()
		);

		var urlBuilder = new UriBuilder(
			Uri.UriSchemeHttp,
			endpoint.Address.ToString(),
			endpoint.Port,
			configurationService.SignalRSettings.HubPath
		);

		return urlBuilder.Uri;
	}

	private async Task<IPEndPoint?> DiscoverServerAsync(CancellationToken cancellationToken = default)
	{
		const string c_discoveryMessage = "DISCOVER_SERVER";
		const string c_discoveryResponse = "SERVER_ADDRESS:";

		using var udpClient = new UdpClient();
		udpClient.EnableBroadcast = true;

		byte[] messageBytes = Encoding.UTF8.GetBytes(c_discoveryMessage);

		var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, configurationService.SignalRSettings.UdpPort);

		await udpClient.SendAsync(messageBytes, messageBytes.Length, broadcastEndpoint);
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(TimeSpan.FromSeconds(5));

		IPEndPoint? endpoint = null;
		try
		{
			UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
			string response = Encoding.UTF8.GetString(result.Buffer);

			if (response.StartsWith(c_discoveryResponse))
			{
				int serverPort = int.Parse(response.Split(':')[1]);

				endpoint = new IPEndPoint(result.RemoteEndPoint.Address, serverPort);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception e)
		{
			logger.LogError(e, "Unexpected exception caught while attempting to discover hub through UDP.");
		}

		return endpoint;
	}
}