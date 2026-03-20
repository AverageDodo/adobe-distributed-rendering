using DistributedRendering.AME.Frontend.Lib.Configuration;

using Microsoft.Extensions.Primitives;

namespace DistributedRendering.AME.Frontend.Services;

public interface IConfigurationService : IDisposable
{
	HubCommunicationSettings HubCommunicationSettings { get; }
}

public sealed class ConfigurationService : IConfigurationService
{
	private IConfiguration root;
	private readonly IDisposable onChangeCallback;

	public ConfigurationService(IConfiguration root)
	{
		this.root = root;

		onChangeCallback = root.GetReloadToken().RegisterChangeCallback(
			o =>
			{
				if (o is IConfiguration configuration)
					this.root = configuration;
			}, this);
	}

	public HubCommunicationSettings HubCommunicationSettings =>
		root.GetSection(nameof(Lib.Configuration.HubCommunicationSettings)).Get<HubCommunicationSettings>()
		?? throw new Exception($"Missing configuration section of type {nameof(HubCommunicationSettings)}.");

	public void Dispose()
	{
		onChangeCallback.Dispose();
	}
}