using DistributedRendering.AME.Server.Lib.Interfaces;
using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Interfaces;

namespace DistributedRendering.AME.Server.Services.Interfaces;

public interface IDatabaseService
{
	ValueTask<bool> CreateRenderRequestAsync(IRenderRequest request, CancellationToken token = default);

	ValueTask<DbRequest?> GetRenderRequestAsync(Guid guid, CancellationToken token = default);

	ValueTask<bool> UpdateRenderRequestAsync(IRenderRequest request, CancellationToken token = default);

	ValueTask<bool> MarkRequestCompleteAsync(Guid requestGuid, DateTime completedAt, CancellationToken token = default);

	ValueTask<IRenderFragment?> GetFragmentAsync(Guid fragmentGuid, CancellationToken token = default);

	ValueTask<bool> InsertFragmentAsync(RenderFragment fragment, CancellationToken token = default);

	Task<ICollection<DbFragment>> GetFragmentsForRequestAsync(Guid requestGuid, CancellationToken token = default);

	ValueTask<bool> InsertRenderReportAsync(IRenderReport report, CancellationToken token = default);

	ValueTask<IRenderReport?> GetRenderReportAsync(Guid fragmentGuid, CancellationToken token = default);

	ValueTask<bool> UpdateRenderReportAsync(IRenderReport report, CancellationToken token = default);

	ValueTask<ICollection<DbReport>> GetAllReportsForRequestAsync(
		Guid requestGuid,
		CancellationToken token = default
	);
}