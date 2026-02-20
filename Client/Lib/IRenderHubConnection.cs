using DistributedRendering.AME.Shared.Interfaces;

namespace DistributedRendering.AME.Client.Lib;

internal interface IRenderHubConnection
{
	Task SubmitRenderReport(IRenderReport report, CancellationToken token = default);
}