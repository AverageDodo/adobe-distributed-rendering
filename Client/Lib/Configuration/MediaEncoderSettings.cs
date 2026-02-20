namespace DistributedRendering.AME.Client.Lib.Configuration;

public sealed record MediaEncoderSettings
{
	public string? MediaEncoderExecutablePath { get; init; }
	public required string ProcessArguments { get; init; }
	public required int KillProcessAfterSeconds { get; init; }
	public TimeSpan ProcessTimeoutInterval => TimeSpan.FromSeconds(KillProcessAfterSeconds);
}