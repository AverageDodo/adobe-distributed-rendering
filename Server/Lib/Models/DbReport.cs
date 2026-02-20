using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Server.Lib.Models;

[DatabaseTable("report")]
public readonly record struct DbReport
{
	[DatabaseName("guid", 0)]
	public required Guid Guid { get; init; }

	[DatabaseName("fk_fragment_guid", 1)]
	public required Guid FragmentGuid { get; init; }

	[DatabaseName("status", 2)]
	public required RenderStatus Status { get; init; }

	[DatabaseName("error_message", 3)]
	public required string? ErrorMessage { get; init; }

	[DatabaseName("fragment_received", 4)]
	public DateTime FragmentReceived { get; init; }

	[DatabaseName("loading_started", 5)]
	public DateTime LoadingStarted { get; init; }

	[DatabaseName("render_started", 6)]
	public DateTime RenderStarted { get; init; }

	[DatabaseName("encoding_started", 7)]
	public DateTime EncodingStarted { get; init; }

	[DatabaseName("export_completed", 8)]
	public DateTime ExportCompleted { get; init; }
}