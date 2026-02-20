namespace DistributedRendering.AME.Client.Lib.Configuration;

public sealed record SignalRSettings
{
	public required int UdpPort { get; init; }
	public required string HubPath { get; init; }
	public required int KeepAliveIntervalInSeconds { get; init; }
	public TimeSpan KeepAliveInterval => TimeSpan.FromSeconds(KeepAliveIntervalInSeconds);
}