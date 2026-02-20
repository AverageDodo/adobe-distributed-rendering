namespace DistributedRendering.AME.Server.Lib.Models;

[DatabaseTable("fragment")]
public sealed record DbFragment
{
	[DatabaseName("guid", 0)]
	public required Guid Guid { get; init; }

	[DatabaseName("fk_request_guid", 1)]
	public required Guid RequestGuid { get; init; }

	[DatabaseName("outputPath", 2)]
	public required FileInfo OutputPath { get; init; }

	[DatabaseName("position", 3)]
	public required int Position { get; init; }

	[DatabaseName("start_time", 4)]
	public required long StartTimeInMilliseconds { get; init; }

	[DatabaseName("duration", 5)]
	public required long DurationInMilliseconds { get; init; }
}