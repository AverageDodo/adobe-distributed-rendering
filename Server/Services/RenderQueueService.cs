using System.Collections.Concurrent;

using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;

namespace DistributedRendering.AME.Server.Services;

internal sealed class RenderQueueService(ILogger<RenderQueueService> logger) : IRenderQueueService
{
	private readonly SemaphoreSlim fragmentSemaphore = new(0);

	// stores the GUIDs of requests where all fragments have been rendered
	private readonly ConcurrentQueue<Guid> reassemblyQueue = [];

	private readonly SemaphoreSlim reassemblySemaphore = new(0);

	// Stores all waiting render jobs
	private readonly ConcurrentQueue<IRenderFragment> renderQueue = [];

	public void QueueFragment(IRenderFragment data)
	{
		renderQueue.Enqueue(data);
		fragmentSemaphore.Release();
	}

	public async Task<IRenderFragment?> DequeueFragmentAsync(CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return null;

		await fragmentSemaphore.WaitAsync(token);

		renderQueue.TryDequeue(out IRenderFragment? data);

		return data;
	}

	public void QueueReassembly(Guid renderRequestGuid)
	{
		reassemblyQueue.Enqueue(renderRequestGuid);
		reassemblySemaphore.Release();
	}

	public async Task<Guid> DequeueReassembly(CancellationToken token = default)
	{
		await reassemblySemaphore.WaitAsync(token);

		if (!reassemblyQueue.TryDequeue(out Guid guid))
			logger.LogWarning("Failed to dequeue GUID from reassembly queue.");

		return guid;
	}
}