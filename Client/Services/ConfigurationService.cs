using DistributedRendering.AME.Client.Lib.Configuration;
using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.Util;

namespace DistributedRendering.AME.Client.Services;

public sealed class ConfigurationService(IConfiguration configuration) : IConfigurationService
{
	public IConfiguration RootConfiguration { get; init; } = configuration;

	public MediaEncoderSettings MediaEncoderSettings { get; init; } =
		configuration.GetSection(nameof(MediaEncoderSettings)).Get<MediaEncoderSettings>()
		?? throw new MissingConfigurationException(typeof(MediaEncoderSettings));

	public SignalRSettings SignalRSettings { get; init; } =
		configuration.GetSection(nameof(SignalRSettings)).Get<SignalRSettings>()
		?? throw new MissingConfigurationException(typeof(SignalRSettings));

	public FileSettings FileSettings { get; init; } =
		configuration.GetSection(nameof(FileSettings)).Get<FileSettings>()
		?? throw new MissingConfigurationException(typeof(FileSettings));

	public DirectoryInfo ProgramDataDirectory { get; init; } =
		new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AME Render Client"));
}