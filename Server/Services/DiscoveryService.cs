using System.Net.Sockets;
using System.Text;

using DistributedRendering.AME.Server.Services.Interfaces;

namespace DistributedRendering.AME.Server.Services;

internal sealed class DiscoveryService(
	ILogger<DiscoveryService> logger,
	IConfigurationService configurationService
) : BackgroundService
{
	private readonly UdpClient discoveryClient = new(configurationService.NetworkSettings.UdpPort);
	private NetworkSettings NetworkSettings => configurationService.NetworkSettings;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			discoveryClient.EnableBroadcast = true;

			while (!stoppingToken.IsCancellationRequested)
				try
				{
					UdpReceiveResult result = await discoveryClient.ReceiveAsync(stoppingToken);
					string message = Encoding.UTF8.GetString(result.Buffer);

					if (message.Equals("DISCOVER_SERVER"))
					{
						logger.LogTrace("Discovery ping received from {Address}", result.RemoteEndPoint);

						var response = $"SERVER_ADDRESS:{NetworkSettings.HubPort}";
						byte[] responseBytes = Encoding.UTF8.GetBytes(response);

						await discoveryClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
						logger.LogTrace("Discovery response sent to {Address}", result.RemoteEndPoint);
					}
				}
				catch (Exception e) when (e is not OperationCanceledException)
				{
					logger.LogError(e, "Exception caught while listening for UPD discovery pings.");
				}
		}
		catch (OperationCanceledException) { }
	}
}