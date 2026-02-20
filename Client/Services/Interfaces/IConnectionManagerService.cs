using Microsoft.AspNetCore.SignalR.Client;

namespace DistributedRendering.AME.Client.Services.Interfaces;

public interface IConnectionManagerService
{
	Task<HubConnection?> CreateHubConnectionAsync(CancellationToken cancellationToken = default);
}