using DistributedRendering.AME.Server.Lib.Interfaces;

namespace DistributedRendering.AME.Server.Services.Interfaces;

public interface IRequestSplittingService
{
	Task SplitRequestAsync(IRenderRequest request, TimeSpan jobLength, CancellationToken token = default);
}