namespace DistributedRendering.AME.Frontend.Lib;

public interface IHubResponse<out TResponse> : IDisposable
{
	bool WasSuccess { get; }

	TResponse? Response { get; }

	Exception? Exception { get; }

	string? ErrorMessage { get; }
}

internal sealed class HubResponse<TResponse> : IHubResponse<TResponse>
{
	public bool WasSuccess => Response != null && Exception == null;
	public TResponse? Response { get; init; }
	public Exception? Exception { get; init; }

	public string? ErrorMessage { get; init; }

	public HubResponse(TResponse response)
	{
		Response = response;
	}

	public HubResponse(Exception e, string? errorMessage = null)
	{
		Exception = e;
		ErrorMessage = errorMessage;
	}

	public void Dispose()
	{
		if (Response is IDisposable disposable)
			disposable.Dispose();
	}
}