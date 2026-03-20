using System.Text;
using System.Text.Json;

using DistributedRendering.AME.Frontend.Lib;
using DistributedRendering.AME.Frontend.Lib.Configuration;

namespace DistributedRendering.AME.Frontend.Services;

public interface IHubCommunicationService : IDisposable
{
	Task<IHubResponse<TResponse>> SubmitRenderJobAsync<TResponse>(
		string projectPath,
		TimeSpan timelineLength,
		TimeSpan fragmentDuration,
		CancellationToken token = default
	);

	Task<IHubResponse<TResponse>> GetStatisticsAsync<TResponse>(Guid requestGuid, CancellationToken token = default);
}

public sealed class HubCommunicationService(
	ILogger<HubCommunicationService> logger,
	IConfigurationService configurationService
) : IHubCommunicationService
{
	private readonly HttpClient httpClient = new();
	private HubCommunicationSettings Config => configurationService.HubCommunicationSettings;

	public async Task<IHubResponse<TResponse>> SubmitRenderJobAsync<TResponse>(
		string projectPath,
		TimeSpan timelineLength,
		TimeSpan fragmentDuration,
		CancellationToken token = default
	)
	{
		using var multipartForm = new MultipartFormDataContent();
		multipartForm.Add(new StringContent(projectPath, Encoding.UTF8), Endpoints.c_ProjectPath);
		multipartForm.Add(new StringContent(timelineLength.ToString("c"), Encoding.UTF8), Endpoints.c_TimelineLength);
		multipartForm.Add(new StringContent(fragmentDuration.ToString("c"), Encoding.UTF8), Endpoints.c_FragmentDuration);

		HubResponse<TResponse> responseObj = null!;
		try
		{
			using HttpResponseMessage response = await httpClient.PostAsync(
				requestUri: Config.PostRenderJobEndpoint,
				content: multipartForm,
				cancellationToken: token
			);

			string responseContent = await response.Content.ReadAsStringAsync(token);

			if (!response.IsSuccessStatusCode)
			{
				logger.LogWarning(
					"Received non-success status code from endpoint. Status code was '{Code}', message was '{Message}'.",
					response.StatusCode,
					responseContent
				);
			}
			else
			{
				var content = JsonSerializer.Deserialize<TResponse>(responseContent, SerializerOptions);

				ArgumentNullException.ThrowIfNull(content, $"Failed to deserialize response object of type {typeof(TResponse)}.");

				responseObj = new HubResponse<TResponse>(content);
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to send new render job to Hub.");

			responseObj = new HubResponse<TResponse>(e, e.Message);
		}

		return responseObj;
	}

	public Task<IHubResponse<TResponse>> GetStatisticsAsync<TResponse>(Guid requestGuid, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		httpClient.Dispose();
	}
}