namespace DistributedRendering.AME.Server.Lib.Models;

[DatabaseTable("request")]
public sealed record DbRequest
{
	[DatabaseName("guid", 0)]
	public required Guid Guid { get; init; }

	[DatabaseName("requestDirectory", 1)]
	public required DirectoryInfo RequestDirectory { get; init; }

	[DatabaseName("projectPath", 2)]
	public required FileInfo ProjectFile { get; init; }

	[DatabaseName("encoderPresetPath", 3)]
	public required FileInfo EncoderPresetFile { get; init; }

	[DatabaseName("outputPath", 4)]
	public required FileInfo OutputPath { get; init; }

	[DatabaseName("timelineLength", 5)]
	public required TimeSpan TimelineLength { get; init; }

	[DatabaseName("createdAt", 6)]
	public required DateTime CreatedAt { get; init; }

	[DatabaseName("completedAt", 7)]
	public DateTime? CompletedAt { get; set; }
}