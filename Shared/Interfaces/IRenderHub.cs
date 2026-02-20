using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Shared.Interfaces;

public interface IRenderHub
{
	Task OnClientStatusUpdate(ClientStatus updatedStatus);

    /// <summary>
    ///     This method is triggered by the client after the render process exits. The string parameter is
    ///     simply a serialized instance of <see cref="IRenderReport" /> since SignalR refuses to properly
    ///     serialize this type on its own.
    /// </summary>
    /// <param name="reportJson">A serialized instance of <see cref="IRenderReport" />.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveRenderReport(string reportJson);
}