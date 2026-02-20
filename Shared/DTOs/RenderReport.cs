using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;

namespace DistributedRendering.AME.Shared.DTOs;

public class RenderReport(
	RenderFragment fragment
) : IRenderReport
{
	public required Guid Guid { get; init; }
	public RenderFragment Fragment { get; init; } = fragment;
	public RenderStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime FragmentReceived { get; set; }
	public DateTime LoadingStarted { get; set; }
	public DateTime RenderStarted { get; set; }
	public DateTime EncodingStarted { get; set; }
	public DateTime ExportCompleted { get; set; }
}