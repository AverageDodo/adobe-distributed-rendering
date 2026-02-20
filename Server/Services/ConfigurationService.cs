using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.Util;

namespace DistributedRendering.AME.Server.Services;

internal sealed class ConfigurationService(IConfiguration configuration) : IConfigurationService
{
	public IConfiguration RootConfiguration { get; } = configuration;

	public RenderHubSettings RenderHubSettings { get; } =
		configuration.GetSection(nameof(Interfaces.RenderHubSettings)).Get<RenderHubSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(Interfaces.RenderHubSettings)}' was not found in the configuration."
		);

	public DatabaseSettings DatabaseSettings { get; } =
		configuration.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(DatabaseSettings)}' was not found in the configuration."
		);

	public FFMpegSettings FFMpegSettings { get; } =
		configuration.GetSection(nameof(FFMpegSettings)).Get<FFMpegSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(FFMpegSettings)}' was not found in the configuration."
		);

	public FileSettings FileSettings { get; } =
		configuration.GetSection(nameof(FileSettings)).Get<FileSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(FileSettings)}' was not found in the configuration."
		);

	public RenderSchedulerSettings RenderSchedulerSettings { get; } =
		configuration.GetSection(nameof(RenderSchedulerSettings)).Get<RenderSchedulerSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(RenderSchedulerSettings)}' was not found in the configuration."
		);

	public NetworkSettings NetworkSettings { get; } =
		configuration.GetSection(nameof(NetworkSettings)).Get<NetworkSettings>()
		?? throw new MissingConfigurationException(
			$"The required section '{nameof(NetworkSettings)}' was not found in the configuration."
		);
}