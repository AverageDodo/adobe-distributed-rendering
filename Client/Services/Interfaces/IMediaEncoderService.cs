using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Interfaces;

namespace DistributedRendering.AME.Client.Services.Interfaces;

internal interface IMediaEncoderService : IDisposable, IAsyncDisposable
{
	event EventHandler<IRenderReport> RenderCompleteEvent;

	Task<bool> StartRenderProcessAsync(IRenderFragment data, CancellationToken token = default);
}