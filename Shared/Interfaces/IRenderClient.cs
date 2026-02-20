using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Shared.Interfaces;

public interface IRenderClient
{
    Task<ClientStatus> GetClientStatus();

    Task<bool> StartRenderJob(IRenderFragment data);
}