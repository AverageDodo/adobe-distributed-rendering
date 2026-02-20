// ReSharper disable InconsistentNaming

namespace DistributedRendering.AME.Server.Services.Interfaces;

public interface IConfigurationService
{
	IConfiguration RootConfiguration { get; }
	RenderHubSettings RenderHubSettings { get; }
	DatabaseSettings DatabaseSettings { get; }
	FFMpegSettings FFMpegSettings { get; }
	FileSettings FileSettings { get; }
	RenderSchedulerSettings RenderSchedulerSettings { get; }
	NetworkSettings NetworkSettings { get; }
}

public sealed record RenderSchedulerSettings(
	short LoopTimeoutInMilliseconds
)
{
	public TimeSpan LoopTimeout { get; } = TimeSpan.FromMilliseconds(LoopTimeoutInMilliseconds);
}

public sealed record RenderHubSettings(
	int RenderFragmentLengthInSeconds
)
{
	public TimeSpan JobLength => TimeSpan.FromSeconds(RenderFragmentLengthInSeconds);
}

public sealed record DatabaseSettings(
	string ConnectionString,
	uint MaxRowCount,
	bool ClearDatabaseOnRestart
);

public sealed record FFMpegSettings(
	string? BinariesFolder = null
);

public sealed record FileSettings(
	string RootDataDirectoryPath,
	string ProjectsDirectoryPath,
	string RequestsDirectoryPath,
	int MaxProjectDirCount,
	long MaxProjectsDirSize,
	bool DeleteFragmentsAfterReassembly
)
{
	public DirectoryInfo RootDataDirectory => new(RootDataDirectoryPath);
	public DirectoryInfo ProjectsDirectory => new(Path.Combine(RootDataDirectoryPath, ProjectsDirectoryPath));
	public DirectoryInfo RequestsDirectory => new(Path.Combine(RootDataDirectoryPath, RequestsDirectoryPath));
}

public sealed record NetworkSettings(
	int UdpPort,
	int HubPort
);

public sealed record FileLoggerSettings(
	bool Enabled,
	string LogFileDirectory,
	short LoopTimeoutInMilliseconds,
	LogLevel MinimumLogLevel,
	int MaxFileSizeInBytes
)
{
	public TimeSpan LoopTimeout { get; } = TimeSpan.FromMilliseconds(LoopTimeoutInMilliseconds);
}

public sealed record DatabaseLoggerSettings(
	bool Enabled,
	string ConnectionString,
	int RowCountLimit,
	LogLevel MinimumLogLevel
);