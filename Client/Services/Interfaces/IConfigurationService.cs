using DistributedRendering.AME.Client.Lib.Configuration;

namespace DistributedRendering.AME.Client.Services.Interfaces;

public interface IConfigurationService
{
	// The init properties need to exist for DI during unit testing
	IConfiguration RootConfiguration { get; init; }
	MediaEncoderSettings MediaEncoderSettings { get; init; }
	SignalRSettings SignalRSettings { get; init; }
	FileSettings FileSettings { get; init; }
	DirectoryInfo ProgramDataDirectory { get; init; }
}