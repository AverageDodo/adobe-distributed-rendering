using DistributedRendering.AME.Server.Lib.Interfaces;

namespace DistributedRendering.AME.Server.Services.Interfaces;

public interface IFileManagementService
{
	bool CreateWatcherForRequest(IRenderRequest request, ICollection<FileInfo> requiredFiles);
}