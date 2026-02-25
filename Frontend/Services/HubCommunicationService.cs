using System.Text;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Options;

using HttpMethod = System.Net.Http.HttpMethod;

namespace Frontend.Services;

public interface IHubCommunicationService : IAsyncDisposable
{
	Task<object?> SubmitRenderJobAsync(
		string projectPath,
		TimeSpan timelineLength,
		TimeSpan fragmentDuration,
		CancellationToken token = default
	);

	Task<object?> GetStatisticsAsync(Guid requestGuid, CancellationToken token = default);
}

public sealed class HubCommunicationService(
	ILogger<HubCommunicationService> logger,
	IConfigurationService configurationService
) : IHubCommunicationService
{
	private readonly HttpClient httpClient = new();
	private HubCommunicationSettings Config => configurationService.HubCommunicationSettings;

	public async Task<object?> SubmitRenderJobAsync(
		string projectPath,
		TimeSpan timelineLength,
		TimeSpan fragmentDuration,
		CancellationToken token = default
	)
	{
		using var multipartForm = new MultipartFormDataContent();
		multipartForm.Add(new StringContent(projectPath, Encoding.UTF8), "projectPath");
		multipartForm.Add(new StringContent(timelineLength.ToString("c"), Encoding.UTF8), "timelineLength");
		multipartForm.Add(new StringContent(fragmentDuration.ToString("c"), Encoding.UTF8), "jobDuration");

		object? responseObj = null;
		try
		{
			using HttpResponseMessage response = await httpClient.PostAsync(
				requestUri: Config.PostRenderJobEndpoint,
				content: multipartForm,
				cancellationToken: token
			);

			if (!response.IsSuccessStatusCode)
			{
				logger.LogWarning(
					"Received non-success status code from endpoint. Status code was '{Code}', message was '{Message}'.",
					response.StatusCode,
					await response.Content.ReadAsStringAsync(token)
				);
			}
			else
			{
				responseObj = await response.Content.ReadAsStringAsync(token);
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to send new render job to Hub.");
		}

		return responseObj;
	}

	public async Task<object?> GetStatisticsAsync(Guid requestGuid, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}

	public ValueTask DisposeAsync()
	{
		httpClient.Dispose();

		return ValueTask.CompletedTask;
	}
}

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