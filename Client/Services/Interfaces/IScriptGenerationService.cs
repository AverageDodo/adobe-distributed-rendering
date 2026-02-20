using DistributedRendering.AME.Shared.DTOs;

namespace DistributedRendering.AME.Client.Services.Interfaces;

internal interface IScriptGenerationService
{
	Task<FileInfo?> GenerateRenderScriptAsync(IRenderFragment jobData, CancellationToken token = default);
}