namespace Frontend.Services;

public interface IConfigurationService
{
	HubCommunicationSettings HubCommunicationSettings { get; }
}

public sealed class ConfigurationService(IConfiguration root) : IConfigurationService
{
	public HubCommunicationSettings HubCommunicationSettings =>
		root.GetSection(nameof(Services.HubCommunicationSettings)).Get<HubCommunicationSettings>()
		?? throw new Exception($"Missing configuration section of type {nameof(HubCommunicationSettings)}.");
}