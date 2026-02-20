using DistributedRendering.AME.Shared.DTOs;

namespace DistributedRendering.AME.Server.Services.Interfaces;

public interface IRenderQueueService
{
	void QueueFragment(IRenderFragment data);

	Task<IRenderFragment?> DequeueFragmentAsync(CancellationToken token = default);

	void QueueReassembly(Guid renderRequestGuid);

	Task<Guid> DequeueReassembly(CancellationToken token = default);
}