namespace DistributedRendering.AME.Server.Lib.Models;

/// <summary>
///     A record struct that calculates the durations of the different stages of a render job based on the timestamps
///     stored in a <see cref="DbReport" />.
/// </summary>
/// <param name="Report">The instance of <see cref="DbReport" /> to use.</param>
public readonly record struct RenderReportTimestamps(
	DbReport Report
)
{
	public bool IsComplete =>
		LoadingTime != TimeSpan.Zero && RenderingTime != TimeSpan.Zero && EncodingTime != TimeSpan.Zero &&
		TimeToExport != TimeSpan.Zero;

	public TimeSpan LoadingTime => Report.RenderStarted - Report.LoadingStarted;
	public TimeSpan RenderingTime => Report.EncodingStarted - Report.RenderStarted;
	public TimeSpan EncodingTime => Report.ExportCompleted - Report.EncodingStarted;
	public TimeSpan TimeToExport => Report.ExportCompleted - Report.FragmentReceived;
}