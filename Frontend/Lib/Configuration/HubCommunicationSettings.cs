namespace DistributedRendering.AME.Frontend.Lib.Configuration;

public sealed record HubCommunicationSettings(
	string Scheme,
	string Host,
	int Port,
	string PostRequestUriPath,
	string GetStatisticsUriPath
)
{
	private Uri BaseUri => new UriBuilder
	{
		Scheme = Scheme,
		Host = Host,
		Port = Port,
	}.Uri;

	public Uri PostRenderJobEndpoint => new UriBuilder(BaseUri) { Path = PostRequestUriPath }.Uri;

	public Uri GetStatisticsEndpoint => new UriBuilder(BaseUri) { Path = GetStatisticsUriPath }.Uri;
}